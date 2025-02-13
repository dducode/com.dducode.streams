using System;
using System.Collections.Generic;
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
    private event Action DisposeEvent;
    private event Action DelayedActions;

    private readonly string _name;
    private float? _streamDeltaTime;
    private float _accumulatedDeltaTime;
    private bool _lock;

    internal ExecutionStream(StreamToken disposeToken, string name) {
      disposeToken.Register(Dispose);
      _name = name;
    }

    public StreamAction Add([NotNull] Action<float> action, StreamToken token = default, uint priority = uint.MaxValue) {
      ValidateAddAction(action);

      var streamAction = new StreamAction(action, priority);
      AddAction(streamAction, float.PositiveInfinity, token);
      return streamAction;
    }

    public StreamAction AddTemporary(float time, [NotNull] Action<float> action, StreamToken token = default, uint priority = uint.MaxValue) {
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
      [NotNull] Func<bool> condition, [NotNull] Action<float> action, StreamToken token = default, uint priority = uint.MaxValue
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

    public StreamAction AddOnce([NotNull] Action action, StreamToken token = default, uint priority = uint.MaxValue) {
      ValidateAddAction(action);

      var streamAction = new StreamAction(_ => action(), priority);
      AddAction(streamAction, float.Epsilon, token);
      return streamAction;
    }

    public StreamAction AddOnce([NotNull] Func<StreamTask> action, StreamToken token = default) {
      ValidateAddAction(action);

      var streamAction = new StreamAction(_ => action(), uint.MaxValue);
      AddAction(streamAction, float.Epsilon, token);
      return streamAction;
    }

    public void AddTimer(float time, [NotNull] Action onComplete, StreamToken token = default) {
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
        if (token.Released)
          return;
        AddOnce(onComplete, token);
      });
    }

    public void SetDelta(float delta) {
      if (delta <= 0)
        throw new ArgumentOutOfRangeException(nameof(delta));
      _streamDeltaTime = delta;
    }

    public void Lock(StreamToken lockToken) {
      _lock = true;
      lockToken.Register(() => _lock = false);
    }

    public void OnDispose(Action onDispose) {
      DisposeEvent += onDispose ?? throw new ArgumentNullException(nameof(onDispose));
    }

    public override string ToString() {
      return _name;
    }

    internal void Update(float deltaTime) {
      ValidateExecution();
      if (!CanExecute())
        return;

      try {
        if (!_streamDeltaTime.HasValue) {
          Execute(deltaTime);
          return;
        }

        _accumulatedDeltaTime += deltaTime;
        if (_accumulatedDeltaTime < _streamDeltaTime.Value)
          return;

        while (_accumulatedDeltaTime > 0) {
          Execute(_streamDeltaTime.Value);
          _accumulatedDeltaTime -= _streamDeltaTime.Value;
        }
      }
      finally {
        DelayedActions?.Invoke();
        DelayedActions = null;
      }
    }

    private void AddAction(StreamAction streamAction, float time, StreamToken token) {
      if (StreamState == State.Running) {
        DelayedActions += () => PerformAddAction(streamAction, time, token);
        return;
      }

      PerformAddAction(streamAction, time, token);
    }

    private void PerformAddAction(StreamAction streamAction, float time, StreamToken token) {
      _actionsStorage.Add(streamAction, time, token);
    }

    private void ValidateExecution() {
      switch (StreamState) {
        case State.Disposed:
          throw new StreamsException("Cannot execute disposed stream");
        case State.Running:
          StreamState = State.Idle;
          Dispose();
          throw new StreamsException("Recursive execution occurred");
        case State.Idle:
          break;
        case State.Disposing:
          break;
        default:
          throw new ArgumentOutOfRangeException();
      }
    }

    private bool CanExecute() {
      if (_actionsStorage.Count == 0)
        return false;

      if (_lock)
        return false;

      return true;
    }

    private void Execute(float deltaTime) {
      _actionsStorage.Refresh();

      StreamState = State.Running;
      Streams.PushStream(this);
      Profiler.BeginSample(_name);

      foreach (KeyValuePair<StreamAction, ActionLifecycle> pair in _actionsStorage) {
        StreamAction action = pair.Key;
        ActionLifecycle lifecycle = pair.Value;

        try {
          action.Invoke(deltaTime, lifecycle.remainingTime);
        }
        catch (Exception exception) {
          Debug.LogError($"An error occured while executing action <b>{action}</b>");
          Debug.LogException(exception);
          _actionsStorage.Remove(action);
        }
      }

      Profiler.EndSample();
      Streams.PopStream();
      StreamState = State.Idle;

      _actionsStorage.TickTime(deltaTime);
    }

    private void Dispose() {
      if (StreamState == State.Running) {
        DelayedActions += Dispose;
        return;
      }

      StreamState = State.Disposing;
      _actionsStorage.Dispose();
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