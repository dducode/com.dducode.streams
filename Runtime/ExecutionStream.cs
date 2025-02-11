using System;
using System.Collections.Generic;
using System.Threading;
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

    private readonly SortedList<StreamAction, ActionLifecycle> _actions = new(new StreamActionComparer());
    private readonly Queue<StreamAction> _actionsQueueToRemove = new();
    private readonly List<StreamAction> _persistentQueueToUpdate = new();
    private bool _rebuildPersistentQueue;

    private event Action DisposeEvent;
    private event Action DelayedActions;

    private readonly CancellationToken _disposeToken;
    private readonly string _name;

    private CancellationToken _lockToken = CancellationToken.None;
    private float? _streamDeltaTime;
    private float _accumulatedDeltaTime;

    internal ExecutionStream(CancellationToken disposeToken, string name) {
      _disposeToken = disposeToken;
      _name = name;
    }

    public StreamAction Add([NotNull] Action<float> action, CancellationToken token = default, uint priority = uint.MaxValue) {
      ValidateAddAction(action);

      var streamAction = new StreamAction(action, priority);
      AddAction(streamAction, float.PositiveInfinity, token);
      return streamAction;
    }

    public StreamAction AddTemporary(float time, [NotNull] Action<float> action, CancellationToken token = default, uint priority = uint.MaxValue) {
      if (time <= 0) {
        Debug.LogWarning($"Time is negative or zero: {time}");
        return null;
      }

      ValidateAddAction(action);

      var streamAction = new StreamAction(action, priority);
      AddAction(streamAction, time, token);
      return streamAction;
    }

    public StreamAction AddConditional(
      [NotNull] Func<bool> condition, [NotNull] Action<float> action, CancellationToken token = default, uint priority = uint.MaxValue
    ) {
      ValidateAddAction(action);
      if (condition == null)
        throw new ArgumentNullException(nameof(condition));

      var streamAction = new StreamAction(deltaTime => {
        if (condition())
          action(deltaTime);
      }, priority);
      AddAction(streamAction, float.PositiveInfinity, token);
      return streamAction;
    }

    public StreamAction AddOnce([NotNull] Action action, CancellationToken token = default, uint priority = uint.MaxValue) {
      ValidateAddAction(action);

      var streamAction = new StreamAction(_ => action(), priority);
      AddAction(streamAction, float.Epsilon, token);
      return streamAction;
    }

    public StreamAction AddOnce([NotNull] Func<StreamTask> action, CancellationToken token = default) {
      ValidateAddAction(action);

      var streamAction = new StreamAction(_ => action(), uint.MaxValue);
      AddAction(streamAction, float.Epsilon, token);
      return streamAction;
    }

    public void AddTimer(float time, [NotNull] Action onComplete, CancellationToken token = default) {
      if (time <= 0) {
        Debug.LogWarning($"Time is negative or zero: {time}");
        return;
      }

      ValidateAddAction(onComplete);

      var streamAction = new StreamAction(_ => { }, uint.MaxValue);
      AddAction(streamAction, time, token);
      streamAction.OnDispose(() => {
        if (StreamState == State.Disposing)
          return;
        if (token.IsCancellationRequested)
          return;
        AddOnce(onComplete, token);
      });
    }

    public void SetDelta(float delta) {
      if (delta <= 0)
        throw new ArgumentOutOfRangeException(nameof(delta));
      _streamDeltaTime = delta;
    }

    public void Lock(CancellationToken lockToken) {
      _lockToken = lockToken;
    }

    public void OnDispose(Action onDispose) {
      DisposeEvent += onDispose ?? throw new ArgumentNullException(nameof(onDispose));
    }

    public override string ToString() {
      return _name;
    }

    internal void Update(float deltaTime) {
      if (StreamState == State.Disposed)
        throw new StreamsException("Cannot execute disposed stream");

      if (_disposeToken.IsCancellationRequested) {
        Dispose();
        return;
      }

      if (StreamState == State.Running) {
        Dispose();
        throw new StreamsException("Recursive execution occurred");
      }

      if (_actions.Count == 0)
        return;

      if (_lockToken.IsCancellationRequested)
        _lockToken = CancellationToken.None;

      if (_lockToken != CancellationToken.None)
        return;

      try {
        if (!_streamDeltaTime.HasValue) {
          PrepareBeforeExecution();
          Execute(deltaTime);
          TickActionsTime(deltaTime);
          return;
        }

        _accumulatedDeltaTime += deltaTime;
        if (_accumulatedDeltaTime < _streamDeltaTime.Value)
          return;

        while (_accumulatedDeltaTime > 0) {
          PrepareBeforeExecution();
          Execute(_streamDeltaTime.Value);
          TickActionsTime(_streamDeltaTime.Value);
          _accumulatedDeltaTime -= _streamDeltaTime.Value;
        }
      }
      finally {
        DelayedActions?.Invoke();
        DelayedActions = null;
      }
    }

    private void AddAction(StreamAction streamAction, float time, CancellationToken token) {
      if (StreamState == State.Running) {
        DelayedActions += () => PerformAddAction(streamAction, time, token);
        return;
      }

      PerformAddAction(streamAction, time, token);
    }

    private void PerformAddAction(StreamAction streamAction, float time, CancellationToken token) {
      _actions.Add(streamAction, new ActionLifecycle(time, token));
      _rebuildPersistentQueue = true;
    }

    private void PrepareBeforeExecution() {
      CheckActionsLifecycle();
      RemoveCompletedActions();
      RebuildPersistentQueue();
    }

    private void CheckActionsLifecycle() {
      foreach ((StreamAction action, ActionLifecycle lifecycle) in _actions)
        if (lifecycle.remainingTime <= 0 || lifecycle.token.IsCancellationRequested)
          _actionsQueueToRemove.Enqueue(action);
    }

    private void RemoveCompletedActions() {
      while (_actionsQueueToRemove.TryDequeue(out StreamAction action)) {
        if (!action.Executed)
          Debug.LogWarning($"Action {action} has not been executed yet");

        _actions.Remove(action);
        _persistentQueueToUpdate.Remove(action);
        action.Dispose();
      }
    }

    private void RebuildPersistentQueue() {
      if (!_rebuildPersistentQueue)
        return;

      _persistentQueueToUpdate.Clear();

      foreach (StreamAction action in _actions.Keys)
        _persistentQueueToUpdate.Add(action);

      _rebuildPersistentQueue = false;
    }

    private void Execute(float deltaTime) {
      StreamState = State.Running;
      Streams.PushStream(this);
      Profiler.BeginSample(_name);

      foreach (StreamAction action in _persistentQueueToUpdate) {
        try {
          action.Invoke(deltaTime, _actions[action].remainingTime);
        }
        catch (Exception exception) {
          Debug.LogError($"An error occured while executing action <b>{action}</b>");
          Debug.LogException(exception);
          _actionsQueueToRemove.Enqueue(action);
        }
      }

      Profiler.EndSample();
      Streams.PopStream();
      StreamState = State.Idle;
    }

    private void TickActionsTime(float deltaTime) {
      foreach (ActionLifecycle lifecycle in _actions.Values)
        lifecycle.remainingTime -= deltaTime;
    }

    private void Dispose() {
      StreamState = State.Disposing;
      foreach (StreamAction action in _actions.Keys)
        action.Dispose();
      _actions.Clear();
      DisposeEvent?.Invoke();
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