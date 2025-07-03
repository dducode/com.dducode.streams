using System;
using Streams.StreamActions.Components;
using UnityEngine;

namespace Streams.StreamActions {

  internal sealed class StreamAction : StreamActionBase, IConfigurable {

    public uint Priority {
      get => _priority;
      private set {
        _priority = value;
        OnPriorityChanged?.Invoke();
      }
    }

    public event Action OnPriorityChanged;

    private bool IsLocked => _lockers > 0;
    private readonly Configuration _configuration = new();
    private readonly Action<float> _action;

    private ulong _ticks;
    private float _accumulatedDeltaTime;
    private uint _priority = uint.MaxValue;

    private int _lockers;
    private readonly Action _lockersDecrement;

    internal StreamAction(Action<float> action, StreamToken cancellationToken) : base(cancellationToken) {
      _action = action;
      _lockersDecrement = () => _lockers--;
    }

    public IConfigurable SetDelta(float value) {
      _configuration.Delta = value;
      _accumulatedDeltaTime = 0;
      return this;
    }

    public IConfigurable ResetDelta() {
      _configuration.ResetDelta();
      _accumulatedDeltaTime = 0;
      return this;
    }

    public IConfigurable SetTickRate(uint value) {
      _configuration.TickRate = value;
      _accumulatedDeltaTime = 0;
      return this;
    }

    public IConfigurable SetPriority(uint value) {
      Priority = value;
      return this;
    }

    public void Lock(StreamToken lockToken) {
      _lockers++;
      lockToken.Register(_lockersDecrement);
    }

    public override void Invoke(float deltaTime) {
      base.Invoke(deltaTime);
      if (IsLocked)
        return;

      _ticks++;
      _accumulatedDeltaTime += deltaTime;

      if (!_configuration.HasDelta) {
        if (_ticks % _configuration.TickRate == 0) {
          _action(_accumulatedDeltaTime);
          _accumulatedDeltaTime = 0;
        }

        return;
      }

      while (_accumulatedDeltaTime > _configuration.Delta || Mathf.Approximately(_accumulatedDeltaTime, _configuration.Delta)) {
        _action(_configuration.Delta);
        _accumulatedDeltaTime -= _configuration.Delta;
      }
    }

  }

}