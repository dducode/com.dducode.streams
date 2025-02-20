using System;
using JetBrains.Annotations;
using StreamsForUnity.Internal;
using StreamsForUnity.StreamTasks;
using UnityEngine.Profiling;
using Debug = UnityEngine.Debug;

namespace StreamsForUnity {

  public sealed class ExecutionStream {

    public enum State {

      Idle,
      Running,
      Disposing,
      Disposed

    }

    public State StreamState { get; private set; }

    private readonly ActionsStorage _actionsStorage = new();
    private readonly ActionsStorage _parallelActionsStorage = new();
    private readonly ParallelActionsWorker _worker = new();
    private readonly Action<float, int> _handleParallelAction;
    private event Action DisposeEvent;
    private event Action DelayedActions;

    private readonly string _name;
    private readonly string _profilerName;
    private bool _locked;

    internal ExecutionStream(StreamToken disposeToken, string name) {
      disposeToken.Register(Dispose);
      _name = name;
      _profilerName = $"{_name} (stream)";
      _handleParallelAction = HandleParallelAction;
    }

    public StreamAction Add([NotNull] Action<float> action, StreamToken token = default, uint priority = uint.MaxValue) {
      ValidateAddAction(action);

      var streamAction = new StreamAction(action, float.PositiveInfinity, priority);
      _actionsStorage.Add(streamAction, token);
      return streamAction;
    }

    public StreamAction AddParallel([NotNull] Action<float> action, StreamToken token = default) {
      ValidateAddAction(action);

      var streamAction = new StreamAction(action, float.PositiveInfinity, uint.MaxValue);
      _parallelActionsStorage.Add(streamAction, token);
      return streamAction;
    }

    public StreamAction AddTemporary(float time, [NotNull] Action<float> action, StreamToken token = default, uint priority = uint.MaxValue) {
      if (time <= 0) {
        Debug.LogWarning($"Time is negative or zero: {time}");
        return null;
      }

      ValidateAddAction(action);

      var streamAction = new StreamAction(action, time, priority);
      _actionsStorage.Add(streamAction, token);
      return streamAction;
    }

    public StreamAction AddConditional(
      [NotNull] Func<bool> condition, [NotNull] Action<float> action, StreamToken token = default, uint priority = uint.MaxValue
    ) {
      ValidateAddAction(action);
      if (condition == null)
        throw new ArgumentNullException(nameof(condition));

      var sts = new StreamTokenSource();
      var streamAction = new StreamAction(deltaTime => {
        if (condition())
          action(deltaTime);
        else
          sts.Release();
      }, float.PositiveInfinity, priority);
      token.Register(sts.Release);
      _actionsStorage.Add(streamAction, sts.Token);
      return streamAction;
    }

    public void AddOnce([NotNull] Action action, StreamToken token = default, uint priority = uint.MaxValue) {
      ValidateAddAction(action);

      var streamAction = new StreamAction(_ => action(), float.Epsilon, priority);
      _actionsStorage.Add(streamAction, token);
    }

    public void AddOnce([NotNull] Func<StreamTask> action, StreamToken token = default) {
      ValidateAddAction(action);

      var streamAction = new StreamAction(_ => action(), float.Epsilon, uint.MaxValue);
      _actionsStorage.Add(streamAction, token);
    }

    public void AddTimer(float time, [NotNull] Action onComplete, StreamToken token = default) {
      if (time <= 0) {
        Debug.LogWarning($"Time is negative or zero: {time}");
        return;
      }

      ValidateAddAction(onComplete);

      var streamAction = new StreamAction(_ => { }, time, uint.MaxValue);
      _actionsStorage.Add(streamAction, token);
      streamAction.OnDispose(() => {
        if (StreamState == State.Disposing)
          return;
        if (token.Released)
          return;
        AddOnce(onComplete, token);
      });
    }

    public void Lock(StreamToken lockToken) {
      _locked = true;
      lockToken.Register(() => _locked = false);
    }

    public void OnDispose(Action onDispose) {
      DisposeEvent += onDispose ?? throw new ArgumentNullException(nameof(onDispose));
    }

    public override string ToString() {
      return _name;
    }

    internal void Join(ExecutionStream other) {
      ExecutionStream runningStream = Streams.RunningStream;
      if (runningStream == this || runningStream == other)
        throw new StreamsException($"Cannot join a running stream ({runningStream})");

      _actionsStorage.Join(other._actionsStorage);
      _parallelActionsStorage.Join(other._parallelActionsStorage);
      DelayedActions += other.DelayedActions;
      DisposeEvent += other.DisposeEvent;
      other.SilentDispose();
    }

    internal void Update(float deltaTime) {
      ValidateExecution();
      _actionsStorage.Refresh();
      _parallelActionsStorage.Refresh();
      if (!CanExecute())
        return;

      try {
        Execute(deltaTime);
      }
      finally {
        DelayedActions?.Invoke();
        DelayedActions = null;
      }
    }

    private void ValidateExecution() {
      switch (StreamState) {
        case State.Disposing or State.Disposed:
          throw new StreamsException("Cannot execute disposed stream");
        case State.Running:
          StreamState = State.Disposing;
          Dispose();
          throw new StreamsException("Recursive execution occurred");
        case State.Idle:
          break;
        default:
          throw new ArgumentOutOfRangeException();
      }
    }

    private bool CanExecute() {
      if (_actionsStorage.Count == 0 && _parallelActionsStorage.Count == 0)
        return false;

      if (_locked)
        return false;

      return true;
    }

    private void Execute(float deltaTime) {
      StreamState = State.Running;
      Streams.PushStream(this);
      Profiler.BeginSample(_profilerName);

      if (_parallelActionsStorage.Count > 0)
        _worker.Start(deltaTime, _parallelActionsStorage.Count, _handleParallelAction);

      for (var i = 0; i < _actionsStorage.Count; i++)
        HandleAction(deltaTime, _actionsStorage, i);

      _worker.Wait();

      Profiler.EndSample();
      Streams.PopStream();
      StreamState = State.Idle;
    }

    private void HandleParallelAction(float deltaTime, int index) {
      HandleAction(deltaTime, _parallelActionsStorage, index);
    }

    private void HandleAction(float deltaTime, ActionsStorage storage, int index) {
      StreamAction action = storage[index];

      try {
        action.Invoke(deltaTime);
      }
      catch (Exception exception) {
        Debug.LogError($"An error occured while executing action <b>{action}</b>");
        Debug.LogException(exception);
        storage.Remove(action);
      }
    }

    private void Dispose() {
      if (StreamState == State.Running) {
        DelayedActions += Dispose;
        return;
      }

      StreamState = State.Disposing;
      _actionsStorage.Dispose();
      _parallelActionsStorage.Dispose();
      DisposeEvent?.Invoke();
      DisposeEvent = null;
      StreamState = State.Disposed;
    }

    private void SilentDispose() {
      DelayedActions = null;
      DisposeEvent = null;
      StreamState = State.Disposed;
    }

    private void ValidateAddAction(Delegate action) {
      switch (StreamState) {
        case State.Disposing or State.Disposed:
          throw new StreamsException("Unable to add action to disposed stream");
      }

      if (action == null)
        throw new ArgumentNullException(nameof(action));
    }

  }

}