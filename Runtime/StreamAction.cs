using System;
using JetBrains.Annotations;

namespace StreamsForUnity {

  public class StreamAction {

    public string Name {
      get => _name;
      set {
        if (string.IsNullOrEmpty(value))
          throw new ArgumentNullException(nameof(Name));
        _name = value;
      }
    }

    public bool Executed { get; private set; }

    private event Action DisposeEvent;
    private readonly Action<float> _action;
    private Predicate<float> _condition;
    private float? _actionDeltaTime;
    private float _accumulatedDeltaTime;
    private string _name = nameof(StreamAction);

    internal StreamAction(Action<float> action) {
      _action = action;
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

    internal void Invoke(float deltaTime, float remainingTime) {
      if (!_actionDeltaTime.HasValue) {
        try {
          _action.Invoke(deltaTime);
        }
        finally {
          Executed = true;
        }

        return;
      }

      _accumulatedDeltaTime += Math.Min(deltaTime, remainingTime);
      if (_accumulatedDeltaTime < _actionDeltaTime.Value)
        return;

      while (_accumulatedDeltaTime > 0) {
        try {
          _action.Invoke(_actionDeltaTime.Value);
        }
        finally {
          _accumulatedDeltaTime -= _actionDeltaTime.Value;
          Executed = true;
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