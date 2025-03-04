using System;
using StreamsForUnity.StreamActions.Components;
using UnityEngine;

namespace StreamsForUnity.StreamActions {

  public sealed class ConditionalStreamAction : StreamAction, IConfigurable<ConditionalStreamAction> {

    private protected override Delegate Action => _action;

    private readonly Action<float> _action;
    private readonly Configuration _configuration = new();

    private readonly Func<bool> _condition;
    private ulong _ticks;
    private float _accumulatedDeltaTime;

    internal ConditionalStreamAction(Action<float> action, Func<bool> condition, StreamToken cancellationToken, uint priority) :
      base(cancellationToken, priority) {
      _action = action;
      _condition = condition;
    }

    public ConditionalStreamAction SetDelta(float value) {
      _configuration.Delta = value;
      _accumulatedDeltaTime = 0;
      return this;
    }

    public ConditionalStreamAction ResetDelta() {
      _configuration.ResetDelta();
      _accumulatedDeltaTime = 0;
      return this;
    }

    public ConditionalStreamAction SetTickRate(uint value) {
      _configuration.TickRate = value;
      return this;
    }

    internal override void Invoke(float deltaTime) {
      if (Canceled())
        return;

      _ticks++;

      if (!_configuration.HasDelta) {
        if (_ticks % _configuration.TickRate == 0 && _condition())
          _action(deltaTime);
        return;
      }

      _accumulatedDeltaTime += deltaTime;

      while (_accumulatedDeltaTime > _configuration.Delta || Mathf.Approximately(_accumulatedDeltaTime, _configuration.Delta)) {
        if (_condition())
          _action(_configuration.Delta);
        _accumulatedDeltaTime -= _configuration.Delta;
      }
    }

  }

}