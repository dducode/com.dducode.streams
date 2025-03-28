using System;
using Streams.StreamActions.Components;
using UnityEngine;

namespace Streams.StreamActions {

  public sealed class PersistentStreamAction : StreamAction, IConfigurable<PersistentStreamAction> {

    private protected override Delegate Action => _action;

    private readonly Action<float> _action;
    private readonly Configuration _configuration = new();

    private ulong _ticks;
    private float _accumulatedDeltaTime;

    internal PersistentStreamAction(Action<float> action, StreamToken cancellationToken, uint priority) : base(cancellationToken, priority) {
      _action = action;
    }

    public PersistentStreamAction SetDelta(float value) {
      _configuration.Delta = value;
      _accumulatedDeltaTime = 0;
      return this;
    }

    public PersistentStreamAction ResetDelta() {
      _configuration.ResetDelta();
      _accumulatedDeltaTime = 0;
      return this;
    }

    public PersistentStreamAction SetTickRate(uint value) {
      _configuration.TickRate = value;
      _accumulatedDeltaTime = 0;
      return this;
    }

    internal override void Invoke(float deltaTime) {
      if (Canceled())
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