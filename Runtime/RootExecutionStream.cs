using System;
using StreamsForUnity.Internal;
using StreamsForUnity.StreamTasks;
using UnityEngine;
using UnityEngine.Profiling;

namespace StreamsForUnity {

  public class RootExecutionStream : IExecutionStream {

    public StreamState StreamState { get; private set; }

    private readonly ActionsStorage _actionsStorage = new();
    private readonly ActionsStorage _parallelActionsStorage = new();
    private readonly ParallelActionsWorker _worker = new();
    private readonly Action<float, int> _handleParallelAction;
    private event Action DisposeEvent;
    private event Action DelayedActions;

    private readonly string _name;
    private readonly string _profilerName;

    public static IManagedExecutionStream New(StreamToken disposeToken, string name) {
      return new ExecutionStream(disposeToken, name);
    }

    internal RootExecutionStream(StreamToken disposeToken, string name) {
      disposeToken.Register(Dispose);
      _name = name;
      _profilerName = $"{_name} (stream)";
      _handleParallelAction = HandleParallelAction;
    }

    public StreamAction Add(Action<float> action, StreamToken token = default, uint priority = uint.MaxValue) {
      ValidateAddAction(action);

      var streamAction = new StreamAction(action, float.PositiveInfinity, priority);
      _actionsStorage.Add(streamAction, token);
      return streamAction;
    }

    public StreamAction AddParallel(Action<float> action, StreamToken token = default) {
      ValidateAddAction(action);

      var streamAction = new StreamAction(action, float.PositiveInfinity, uint.MaxValue);
      _parallelActionsStorage.Add(streamAction, token);
      return streamAction;
    }

    public StreamAction AddTemporary(float time, Action<float> action, StreamToken token = default, uint priority = uint.MaxValue) {
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
      Func<bool> condition, Action<float> action, StreamToken token = default, uint priority = uint.MaxValue
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

    public void AddOnce(Action action, StreamToken token = default, uint priority = uint.MaxValue) {
      ValidateAddAction(action);

      var streamAction = new StreamAction(_ => action(), float.Epsilon, priority);
      _actionsStorage.Add(streamAction, token);
    }

    public void AddOnce(Func<StreamTask> action, StreamToken token = default) {
      ValidateAddAction(action);

      var streamAction = new StreamAction(_ => action(), float.Epsilon, uint.MaxValue);
      _actionsStorage.Add(streamAction, token);
    }

    public void AddTimer(float time, Action onComplete, StreamToken token = default) {
      if (time <= 0) {
        Debug.LogWarning($"Time is negative or zero: {time}");
        return;
      }

      ValidateAddAction(onComplete);

      var streamAction = new StreamAction(_ => { }, time, uint.MaxValue);
      _actionsStorage.Add(streamAction, token);
      streamAction.OnDispose(() => {
        if (StreamState == StreamState.Disposing)
          return;
        if (token.Released)
          return;
        AddOnce(onComplete, token);
      });
    }

    public void OnDispose(Action onDispose) {
      DisposeEvent += onDispose ?? throw new ArgumentNullException(nameof(onDispose));
    }

    public override string ToString() {
      return _name;
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
        case StreamState.Disposing or StreamState.Disposed:
          throw new StreamsException("Cannot execute disposed stream");
        case StreamState.Running:
          StreamState = StreamState.Disposing;
          Dispose();
          throw new StreamsException("Recursive execution occurred");
        case StreamState.Idle:
          break;
        default:
          throw new ArgumentOutOfRangeException();
      }
    }

    private bool CanExecute() {
      return _actionsStorage.Count != 0 || _parallelActionsStorage.Count != 0;
    }

    private void Execute(float deltaTime) {
      StreamState = StreamState.Running;
      Streams.PushStream(this);
      Profiler.BeginSample(_profilerName);

      if (_parallelActionsStorage.Count > 0)
        _worker.Start(deltaTime, _parallelActionsStorage.Count, _handleParallelAction);

      for (var i = 0; i < _actionsStorage.Count; i++)
        HandleAction(deltaTime, _actionsStorage, i);

      _worker.Wait();

      Profiler.EndSample();
      Streams.PopStream();
      StreamState = StreamState.Idle;
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
      if (StreamState == StreamState.Running) {
        DelayedActions += Dispose;
        return;
      }

      StreamState = StreamState.Disposing;
      _actionsStorage.Dispose();
      _parallelActionsStorage.Dispose();
      DisposeEvent?.Invoke();
      DisposeEvent = null;
      StreamState = StreamState.Disposed;
    }

    private void ValidateAddAction(Delegate action) {
      switch (StreamState) {
        case StreamState.Disposing or StreamState.Disposed:
          throw new StreamsException("Unable to add action to disposed stream");
      }

      if (action == null)
        throw new ArgumentNullException(nameof(action));
    }

  }

}