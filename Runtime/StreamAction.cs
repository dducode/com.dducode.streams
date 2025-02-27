using System;
using JetBrains.Annotations;
using UnityEngine;

namespace StreamsForUnity {

  public class StreamAction {

    private static int NextId => ++_nextId;
    private static int _nextId = -1;

    public string Name {
      get => _name;
      set {
        if (string.IsNullOrEmpty(value))
          throw new ArgumentNullException(nameof(Name));
        _name = value;
      }
    }

    internal float RemainingTime { get; private set; }
    internal uint Priority { get; private set; }
    internal int Id { get; } = NextId;

    internal event Action OnPriorityChanged;

    private event Action CompleteEvent;
    private event Action CancelEvent;
    private readonly Action<float> _action;

    private float? _actionDeltaTime;
    private float _accumulatedDeltaTime;

    private uint _tickRate = 1;
    private ulong _ticks;

    private bool _canceled;

    private string _name = nameof(StreamAction);

    internal StreamAction(Action<float> action, float time, StreamToken cancellationToken, uint priority) {
      _action = action;
      RemainingTime = time;
      Priority = priority;
      cancellationToken.Register(() => _canceled = true);
    }

    public StreamAction SetDelta(float delta) {
      if (delta <= 0)
        throw new ArgumentOutOfRangeException(nameof(delta), "Delta should be positive");
      _actionDeltaTime = delta;
      _accumulatedDeltaTime = 0;
      return this;
    }

    public StreamAction ResetDelta() {
      _actionDeltaTime = null;
      _accumulatedDeltaTime = 0;
      return this;
    }

    public StreamAction SetTickRate(uint tickRate) {
      if (tickRate == 0)
        throw new ArgumentOutOfRangeException(nameof(tickRate), "Tick rate cannot be zero");
      if (_actionDeltaTime.HasValue)
        Debug.LogWarning("Tick rate has no effect when delta is set");
      _tickRate = tickRate;
      return this;
    }

    public StreamAction OnComplete([NotNull] Action onComplete) {
      CompleteEvent += onComplete ?? throw new ArgumentNullException(nameof(onComplete));
      return this;
    }

    public StreamAction OnCancel([NotNull] Action onCancel) {
      CancelEvent += onCancel ?? throw new ArgumentNullException(nameof(onCancel));
      return this;
    }

    internal void ChangePriority(uint priority) {
      Priority = priority;
      OnPriorityChanged?.Invoke();
    }

    internal void SetCompletionToken(StreamToken completionToken) {
      completionToken.Register(() => RemainingTime = 0);
    }

    internal void Invoke(float deltaTime) {
      if (_canceled) {
        CancelEvent?.Invoke();
        CancelEvent = null;
        return;
      }

      if (RemainingTime <= 0) {
        CompleteEvent?.Invoke();
        CompleteEvent = null;
        return;
      }

      if (!_actionDeltaTime.HasValue) {
        try {
          if (++_ticks % _tickRate == 0)
            _action.Invoke(deltaTime);
        }
        finally {
          RemainingTime -= deltaTime;
        }

        return;
      }

      _accumulatedDeltaTime += Math.Min(deltaTime, RemainingTime);

      while (_accumulatedDeltaTime > _actionDeltaTime.Value || Mathf.Approximately(_accumulatedDeltaTime, _actionDeltaTime.Value)) {
        try {
          _action.Invoke(_actionDeltaTime.Value);
        }
        finally {
          _accumulatedDeltaTime -= _actionDeltaTime.Value;
          RemainingTime -= _actionDeltaTime.Value;
        }
      }
    }

    public override string ToString() {
      return $"{Name} ({_action.Method.Name})";
    }

  }

}