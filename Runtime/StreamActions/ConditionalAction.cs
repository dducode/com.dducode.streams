using System;
using System.Threading;
using Streams.StreamActions.Components;
using UnityEngine;

namespace Streams.StreamActions {

  public sealed class ConditionalAction : SelfClosingAction<ConditionalAction>, IConfigurable<ConditionalAction> {

    public override float DeltaTime => _configuration.HasDelta ? _configuration.Delta : _accumulatedDeltaTime;

    private readonly Configuration _configuration = new();

    private readonly Func<bool> _condition;
    private ulong _ticks;
    private float _accumulatedDeltaTime;

    internal ConditionalAction(
      Action<ConditionalAction> action,
      Func<bool> condition,
      CancellationToken cancellationToken) : base(action, cancellationToken) {
      _condition = condition;
    }

    public ConditionalAction SetDelta(float value) {
      _configuration.Delta = value;
      _accumulatedDeltaTime = 0;
      return this;
    }

    public ConditionalAction ResetDelta() {
      _configuration.ResetDelta();
      _accumulatedDeltaTime = 0;
      return this;
    }

    public ConditionalAction SetTickRate(uint value) {
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
          if (_condition())
            InvokeAction();
          _accumulatedDeltaTime = 0;
        }

        return;
      }

      while (_accumulatedDeltaTime > _configuration.Delta || Mathf.Approximately(_accumulatedDeltaTime, _configuration.Delta)) {
        if (_condition())
          InvokeAction();
        _accumulatedDeltaTime -= _configuration.Delta;
      }
    }

  }

}