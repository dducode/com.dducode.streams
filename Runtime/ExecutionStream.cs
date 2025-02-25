using System;
using StreamsForUnity.Internal;
using StreamsForUnity.StreamTasks;
using UnityEngine.Profiling;
using Debug = UnityEngine.Debug;

namespace StreamsForUnity {

  public class ExecutionStream {

    public StreamState StreamState { get; private protected set; }

    protected Action disposeCallbacks;
    protected Action delayedCallbacks;

    private protected readonly ActionsStorage actionsStorage = new();
    private protected readonly ActionsStorage parallelActionsStorage = new();

    private readonly ParallelActionsWorker _worker = new();
    private readonly Action<float, int> _handleParallelAction;

    private readonly string _name;
    private readonly string _profilerName;

    internal ExecutionStream(string name) {
      _name = name;
      _profilerName = $"{_name} (stream)";
      _handleParallelAction = HandleParallelAction;
    }

    public StreamAction Add(Action<float> action, StreamToken token = default, uint priority = uint.MaxValue) {
      ValidateAddAction(action);

      var streamAction = new StreamAction(action, float.PositiveInfinity, priority);
      actionsStorage.Add(streamAction, token);
      return streamAction;
    }

    public StreamAction AddParallel(Action<float> action, StreamToken token = default) {
      ValidateAddAction(action);

      var streamAction = new StreamAction(action, float.PositiveInfinity, uint.MaxValue);
      parallelActionsStorage.Add(streamAction, token);
      return streamAction;
    }

    public StreamAction AddTemporary(float time, Action<float> action, StreamToken token = default, uint priority = uint.MaxValue) {
      if (time <= 0) {
        Debug.LogWarning($"Time is negative or zero: {time}");
        return null;
      }

      ValidateAddAction(action);

      var streamAction = new StreamAction(action, time, priority);
      actionsStorage.Add(streamAction, token);
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
      actionsStorage.Add(streamAction, sts.Token);
      return streamAction;
    }

    public void AddOnce(Action action, StreamToken token = default, uint priority = uint.MaxValue) {
      ValidateAddAction(action);

      var streamAction = new StreamAction(_ => action(), float.Epsilon, priority);
      actionsStorage.Add(streamAction, token);
    }

    public void AddOnce(Func<StreamTask> action, StreamToken token = default) {
      ValidateAddAction(action);

      var streamAction = new StreamAction(_ => action(), float.Epsilon, uint.MaxValue);
      actionsStorage.Add(streamAction, token);
    }

    public void AddTimer(float time, Action onComplete, StreamToken token = default) {
      if (time <= 0) {
        Debug.LogWarning($"Time is negative or zero: {time}");
        return;
      }

      ValidateAddAction(onComplete);

      var streamAction = new StreamAction(_ => { }, time, uint.MaxValue);
      actionsStorage.Add(streamAction, token);
      streamAction.OnDispose(() => {
        if (StreamState == StreamState.Disposing)
          return;
        if (token.Released)
          return;
        AddOnce(onComplete, token);
      });
    }

    public void OnDispose(Action onDispose) {
      disposeCallbacks += onDispose ?? throw new ArgumentNullException(nameof(onDispose));
    }

    public override string ToString() {
      return _name;
    }

    internal void Update(float deltaTime) {
      ValidateExecution();
      actionsStorage.Refresh();
      parallelActionsStorage.Refresh();
      if (!CanExecute())
        return;

      try {
        Execute(deltaTime);
      }
      finally {
        delayedCallbacks?.Invoke();
        delayedCallbacks = null;
      }
    }

    protected virtual bool CanExecute() {
      return actionsStorage.Count != 0 || parallelActionsStorage.Count != 0;
    }

    private void ValidateExecution() {
      switch (StreamState) {
        case StreamState.Disposing or StreamState.Disposed:
          throw new StreamsException("Cannot execute disposed stream");
        case StreamState.Running:
          StreamState = StreamState.Disposing;
          Dispose_Internal();
          throw new StreamsException("Recursive execution occurred");
        case StreamState.Idle:
          break;
        default:
          throw new ArgumentOutOfRangeException();
      }
    }

    private void Execute(float deltaTime) {
      StreamState = StreamState.Running;
      Streams.PushStream(this);
      Profiler.BeginSample(_profilerName);

      if (parallelActionsStorage.Count > 0)
        _worker.Start(deltaTime, parallelActionsStorage.Count, _handleParallelAction);

      for (var i = 0; i < actionsStorage.Count; i++)
        HandleAction(deltaTime, actionsStorage, i);

      _worker.Wait();

      Profiler.EndSample();
      Streams.PopStream();
      StreamState = StreamState.Idle;
    }

    private void HandleParallelAction(float deltaTime, int index) {
      HandleAction(deltaTime, parallelActionsStorage, index);
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

    internal void Dispose_Internal() {
      if (StreamState == StreamState.Running) {
        delayedCallbacks += Dispose_Internal;
        return;
      }

      StreamState = StreamState.Disposing;
      actionsStorage.Dispose();
      parallelActionsStorage.Dispose();
      disposeCallbacks?.Invoke();
      disposeCallbacks = null;
      delayedCallbacks = null;
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