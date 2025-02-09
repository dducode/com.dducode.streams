using System;
using System.Collections.Generic;
using System.Threading;
using JetBrains.Annotations;
using StreamsForUnity.Internal;
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

    private readonly SortedDictionary<uint, List<StreamAction>> _actionsByPriority = new();
    private readonly Dictionary<StreamAction, ActionLifecycle> _actionLifecycles = new();
    private readonly HashSet<StreamAction> _actionsToRemove = new();
    private readonly List<StreamAction> _actionsToUpdate = new();

    private event Action DisposeEvent;

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

      var streamAction = new StreamAction(action);
      AddAction(streamAction, float.PositiveInfinity, token, priority);
      return streamAction;
    }

    public StreamAction AddTemporary(float time, [NotNull] Action<float> action, CancellationToken token = default, uint priority = uint.MaxValue) {
      if (time <= 0) {
        Debug.LogWarning($"Time is negative or zero: {time}");
        return null;
      }

      ValidateAddAction(action);

      var streamAction = new StreamAction(action);
      AddAction(streamAction, time, token, priority);
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
      });
      AddAction(streamAction, float.PositiveInfinity, token, priority);
      return streamAction;
    }

    public StreamAction AddOnce([NotNull] Action<float> action, CancellationToken token = default, uint priority = uint.MaxValue) {
      ValidateAddAction(action);

      var streamAction = new StreamAction(action);
      AddAction(streamAction, float.Epsilon, token, priority);
      return streamAction;
    }

    public void AddTimer(float time, [NotNull] Action onComplete, CancellationToken token = default) {
      if (time <= 0) {
        Debug.LogWarning($"Time is negative or zero: {time}");
        return;
      }

      ValidateAddAction(onComplete);

      var streamAction = new StreamAction(_ => { });
      AddAction(streamAction, time, token, uint.MaxValue);
      streamAction.OnDispose(onComplete);
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

      if (_actionLifecycles.Count == 0)
        return;

      if (_lockToken.IsCancellationRequested)
        _lockToken = CancellationToken.None;

      if (_lockToken != CancellationToken.None)
        return;

      Profiler.BeginSample(_name);

      try {
        if (!_streamDeltaTime.HasValue) {
          PrepareBeforeExecution();
          TickActionsTime(deltaTime);
          Execute(deltaTime);
          return;
        }

        _accumulatedDeltaTime += deltaTime;

        if (_accumulatedDeltaTime < _streamDeltaTime.Value)
          return;

        while (_accumulatedDeltaTime > 0) {
          PrepareBeforeExecution();
          TickActionsTime(_streamDeltaTime.Value);
          Execute(_streamDeltaTime.Value);
          _accumulatedDeltaTime -= _streamDeltaTime.Value;
        }
      }
      finally {
        Profiler.EndSample();
      }
    }

    private void AddAction(StreamAction streamAction, float time, CancellationToken token, uint priority) {
      if (!_actionsByPriority.ContainsKey(priority))
        _actionsByPriority.Add(priority, new List<StreamAction>());
      _actionsByPriority[priority].Add(streamAction);
      _actionLifecycles.Add(streamAction, new ActionLifecycle(time, token));
    }

    private void PrepareBeforeExecution() {
      CheckActionsLifecycle();
      RemoveCompletedActions();
      SetupActionsToUpdate();
    }

    private void TickActionsTime(float deltaTime) {
      foreach (ActionLifecycle lifecycle in _actionLifecycles.Values)
        lifecycle.remainingTime -= deltaTime;
    }

    private void Execute(float deltaTime) {
      StreamState = State.Running;

      foreach (StreamAction action in _actionsToUpdate) {
        try {
          action.Invoke(deltaTime, _actionLifecycles[action].remainingTime);
        }
        catch (Exception exception) {
          Debug.LogError($"An error occured while executing action <b>{action}</b>");
          Debug.LogException(exception);
          _actionsToRemove.Add(action);
        }
      }

      _actionsToUpdate.Clear();
      StreamState = State.Idle;
    }

    private void CheckActionsLifecycle() {
      foreach ((StreamAction action, ActionLifecycle lifecycle) in _actionLifecycles)
        if (lifecycle.remainingTime <= 0 || lifecycle.token.IsCancellationRequested)
          _actionsToRemove.Add(action);
    }

    private void RemoveCompletedActions() {
      foreach (StreamAction action in _actionsToRemove) {
        if (!action.Executed)
          Debug.LogWarning($"Action {action} has not been executed yet");
        foreach (List<StreamAction> actions in _actionsByPriority.Values)
          if (actions.Remove(action))
            break;
        _actionLifecycles.Remove(action);
        action.Dispose();
      }

      _actionsToRemove.Clear();
    }

    private void SetupActionsToUpdate() {
      foreach (List<StreamAction> actions in _actionsByPriority.Values)
        _actionsToUpdate.AddRange(actions);
    }

    private void Dispose() {
      StreamState = State.Disposing;
      foreach (StreamAction action in _actionLifecycles.Keys)
        action.Dispose();
      _actionLifecycles.Clear();
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