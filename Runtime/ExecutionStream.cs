using System;
using System.Diagnostics.CodeAnalysis;
using StreamsForUnity.Internal;
using StreamsForUnity.StreamTasks;
using UnityEngine.Profiling;
using Debug = UnityEngine.Debug;

namespace StreamsForUnity {

  public class ExecutionStream {

    public ParallelWorkStrategy ParallelWorkStrategy { get; set; } = ParallelWorkStrategy.Effectively;
    public StreamState State { get; private protected set; }

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

      var streamAction = new StreamAction(action, float.PositiveInfinity, token, priority);
      actionsStorage.Add(streamAction);
      return streamAction;
    }

    public StreamAction AddParallel(Action<float> action, StreamToken token = default) {
      ValidateAddAction(action);

      var streamAction = new StreamAction(action, float.PositiveInfinity, token, uint.MaxValue);
      parallelActionsStorage.Add(streamAction);
      return streamAction;
    }

    public StreamAction AddTemporary(float time, Action<float> action, StreamToken token = default, uint priority = uint.MaxValue) {
      if (time <= 0) {
        Debug.LogWarning($"Time is negative or zero: {time}");
        return null;
      }

      ValidateAddAction(action);

      var streamAction = new StreamAction(action, time, token, priority);
      actionsStorage.Add(streamAction);
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
      }, float.PositiveInfinity, token, priority);
      streamAction.SetCompletionToken(sts.Token);
      actionsStorage.Add(streamAction);
      return streamAction;
    }

    public void AddOnce(Action action, StreamToken token = default, uint priority = uint.MaxValue) {
      ValidateAddAction(action);

      actionsStorage.Add(new StreamAction(_ => action(), float.Epsilon, token, priority));
    }

    public void AddOnce(Func<StreamTask> action, StreamToken token = default) {
      ValidateAddAction(action);

      actionsStorage.Add(new StreamAction(_ => action(), float.Epsilon, token, uint.MaxValue));
    }

    public void AddTimer(float time, Action onComplete, StreamToken token = default) {
      if (time <= 0) {
        Debug.LogWarning($"Time is negative or zero: {time}");
        return;
      }

      ValidateAddAction(onComplete);

      var streamAction = new StreamAction(_ => { }, time, token, uint.MaxValue);
      actionsStorage.Add(streamAction);
      streamAction.OnComplete(() => {
        if (State is StreamState.Disposing or StreamState.Disposed)
          return;
        AddOnce(onComplete, token);
      });
    }

    public void OnDispose([NotNull] Action onDispose) {
      disposeCallbacks += onDispose ?? throw new ArgumentNullException(nameof(onDispose));
    }

    public void RemoveDisposeHandle([NotNull] Action onDispose) {
      disposeCallbacks -= onDispose ?? throw new ArgumentNullException(nameof(onDispose));
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

    internal void Dispose_Internal() {
      switch (State) {
        case StreamState.Disposing or StreamState.Disposed:
          return;
        case StreamState.Running:
          delayedCallbacks += Dispose_Internal;
          return;
      }

      State = StreamState.Disposing;
      actionsStorage.Clear();
      parallelActionsStorage.Clear();
      disposeCallbacks?.Invoke();
      disposeCallbacks = null;
      delayedCallbacks = null;
      State = StreamState.Disposed;
    }

    private void ValidateExecution() {
      switch (State) {
        case StreamState.Disposing or StreamState.Disposed:
          throw new StreamsException("Cannot execute disposed stream");
        case StreamState.Running:
          State = StreamState.Invalid;
          Dispose_Internal();
          throw new StreamsException("Recursive execution occurred");
        case StreamState.Invalid:
          Dispose_Internal();
          throw new StreamsException("Stream is in invalid state");
        case StreamState.Idle:
          break;
        default:
          throw new ArgumentOutOfRangeException();
      }
    }

    private void Execute(float deltaTime) {
      State = StreamState.Running;
      Streams.PushStream(this);
      Profiler.BeginSample(_profilerName);

      _worker.Start(deltaTime, parallelActionsStorage.Count, ParallelWorkStrategy, _handleParallelAction);

      for (var i = 0; i < actionsStorage.Count; i++)
        HandleAction(deltaTime, actionsStorage, i);

      _worker.Wait();

      Profiler.EndSample();
      Streams.PopStream();
      State = StreamState.Idle;
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

    private void ValidateAddAction(Delegate action) {
      switch (State) {
        case StreamState.Disposing or StreamState.Disposed:
          throw new StreamsException("Unable to add action to disposed stream");
      }

      if (action == null)
        throw new ArgumentNullException(nameof(action));
    }

  }

}