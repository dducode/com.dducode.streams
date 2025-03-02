using System;
using StreamsForUnity.StreamActions.Components;
using UnityEngine;

namespace StreamsForUnity.StreamActions {

  public sealed class ConditionalStreamAction : StreamAction, IConfigurable<ConditionalStreamAction>, ICompletable {

    private protected override Delegate Action => _action;

    private readonly Action<float> _action;
    private readonly Configuration _configuration = new();
    private readonly Completion _completion = new();

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

    public void OnComplete(Action onComplete, StreamToken subscriptionToken = default) {
      _completion.OnComplete(onComplete, subscriptionToken);
    }

    internal override void Invoke(float deltaTime) {
      if (Canceled())
        return;

      if (!_condition()) {
        _completion.Complete();
        return;
      }

      _ticks++;

      if (!_configuration.HasDelta) {
        if (_ticks % _configuration.TickRate == 0)
          _action(deltaTime);
        return;
      }

      _accumulatedDeltaTime += deltaTime;

      while (_accumulatedDeltaTime > _configuration.Delta || Mathf.Approximately(_accumulatedDeltaTime, _configuration.Delta)) {
        _action(_configuration.Delta);
        _accumulatedDeltaTime -= _configuration.Delta;
      }
    }

  }

}