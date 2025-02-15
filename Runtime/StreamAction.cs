using System;
using JetBrains.Annotations;

namespace StreamsForUnity {

  public class StreamAction {

    private static ulong NextId => _nextId++;
    private static ulong _nextId;

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
    internal ulong Id { get; } = NextId;
    internal event Action OnPriorityChanged;
    internal event Action OnComplete;

    private event Action DisposeEvent;
    private readonly Action<float> _action;
    private float? _actionDeltaTime;
    private float _accumulatedDeltaTime;
    private string _name = nameof(StreamAction);

    internal StreamAction(Action<float> action, float time, uint priority) {
      _action = action;
      RemainingTime = time;
      Priority = priority;
    }

    public StreamAction SetDelta(float delta) {
      if (delta <= 0)
        throw new ArgumentOutOfRangeException(nameof(delta));
      _actionDeltaTime = delta;
      return this;
    }

    public StreamAction OnDispose([NotNull] Action onDispose) {
      DisposeEvent += onDispose ?? throw new ArgumentNullException(nameof(onDispose));
      return this;
    }

    internal void ChangePriority(uint priority) {
      Priority = priority;
      OnPriorityChanged?.Invoke();
    }

    internal void Invoke(float deltaTime) {
      if (RemainingTime <= 0) {
        OnComplete?.Invoke();
        OnComplete = null;
        return;
      }

      if (!_actionDeltaTime.HasValue) {
        try {
          _action.Invoke(deltaTime);
        }
        finally {
          RemainingTime -= deltaTime;
        }

        return;
      }

      _accumulatedDeltaTime += Math.Min(deltaTime, RemainingTime);
      if (_accumulatedDeltaTime < _actionDeltaTime.Value)
        return;

      while (_accumulatedDeltaTime > 0) {
        try {
          _action.Invoke(_actionDeltaTime.Value);
        }
        finally {
          _accumulatedDeltaTime -= _actionDeltaTime.Value;
          RemainingTime -= _actionDeltaTime.Value;
        }
      }
    }

    internal void Dispose() {
      DisposeEvent?.Invoke();
      DisposeEvent = null;
    }

    public override string ToString() {
      return $"{Name} ({_action.Method.Name})";
    }

  }

}