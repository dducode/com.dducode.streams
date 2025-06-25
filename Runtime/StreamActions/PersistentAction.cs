using System;
using System.Threading;
using Streams.StreamActions.Components;
using UnityEngine;

namespace Streams.StreamActions {

  public sealed class PersistentAction : SelfClosingAction<PersistentAction>, IConfigurable<PersistentAction> {

    public override float DeltaTime => _configuration.HasDelta ? _configuration.Delta : _accumulatedDeltaTime;

    private readonly Configuration _configuration = new();

    private ulong _ticks;
    private float _accumulatedDeltaTime;

    internal PersistentAction(Action<PersistentAction> action, StreamToken cancellationToken) : base(action, cancellationToken) {
    }

    public PersistentAction SetDelta(float value) {
      _configuration.Delta = value;
      _accumulatedDeltaTime = 0;
      return this;
    }

    public PersistentAction ResetDelta() {
      _configuration.ResetDelta();
      _accumulatedDeltaTime = 0;
      return this;
    }

    public PersistentAction SetTickRate(uint value) {
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
          InvokeAction();
          _accumulatedDeltaTime = 0;
        }

        return;
      }

      while (_accumulatedDeltaTime > _configuration.Delta || Mathf.Approximately(_accumulatedDeltaTime, _configuration.Delta)) {
        InvokeAction();
        _accumulatedDeltaTime -= _configuration.Delta;
      }
    }

  }

}