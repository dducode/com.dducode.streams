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
    internal event Action OnComplete;

    private event Action DisposeEvent;
    private readonly Action<float> _action;
    private float? _actionDeltaTime;
    private float _accumulatedDeltaTime;

    private float[] _lods;
    private int _lodIndex;

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

    public StreamAction SetupLevelsOfDetails(params float[] lods) {
      for (var i = 0; i < lods.Length; i++) {
        if (lods[i] <= 0)
          throw new ArgumentOutOfRangeException(nameof(lods), $"LOD level {i} cannot be less than 0");
        if (i < lods.Length - 1 && lods[i] > lods[i + 1])
          Debug.LogWarning($"Value of the LOD level {i} ({lods[i]}) is more than {i + 1} ({lods[i + 1]})");
      }

      if (_lods != null && _lods.Length == lods.Length) {
        for (var i = 0; i < lods.Length; i++)
          _lods[i] = lods[i];
      }
      else {
        _lods = (float[])lods.Clone();
      }

      return this;
    }

    public StreamAction SetLOD(int index) {
      if (index < 0 || index >= _lods.Length)
        throw new ArgumentOutOfRangeException(nameof(index));
      SetDelta(_lods[_lodIndex = index]);
      return this;
    }

    public StreamAction NextLOD() {
      _lodIndex = Math.Min(++_lodIndex, _lods.Length - 1);
      SetDelta(_lods[_lodIndex]);
      return this;
    }

    public StreamAction PrevLOD() {
      _lodIndex = Math.Max(--_lodIndex, 0);
      SetDelta(_lods[_lodIndex]);
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