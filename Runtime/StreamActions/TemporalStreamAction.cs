using System;
using StreamsForUnity.StreamActions.Components;
using UnityEngine;

namespace StreamsForUnity.StreamActions {

  public sealed class TemporalStreamAction : StreamAction, IConfigurable<TemporalStreamAction>, ICompletable {

    private protected override Delegate Action => _action;

    private readonly Action<float> _action;
    private readonly Configuration _configuration = new();
    private readonly Completion _completion = new();

    private float _remainingTime;
    private ulong _ticks;
    private float _accumulatedDeltaTime;

    internal TemporalStreamAction(Action<float> action, float time, StreamToken cancellationToken, uint priority) :
      base(cancellationToken, priority) {
      _action = action;
      _remainingTime = time;
    }

    public TemporalStreamAction SetDelta(float value) {
      _configuration.Delta = value;
      _accumulatedDeltaTime = 0;
      return this;
    }

    public TemporalStreamAction ResetDelta() {
      _configuration.ResetDelta();
      _accumulatedDeltaTime = 0;
      return this;
    }

    public TemporalStreamAction SetTickRate(uint value) {
      _configuration.TickRate = value;
      return this;
    }

    public void OnComplete(Action onComplete, StreamToken subscriptionToken = default) {
      _completion.OnComplete(onComplete, subscriptionToken);
    }

    internal override void Invoke(float deltaTime) {
      if (Canceled())
        return;

      if (_remainingTime == 0) {
        _completion.Complete();
        return;
      }

      _ticks++;

      if (!_configuration.HasDelta) {
        if (_ticks % _configuration.TickRate == 0)
          _action(deltaTime);
        _remainingTime = Math.Max(0, _remainingTime - deltaTime);
        return;
      }

      _accumulatedDeltaTime += Math.Min(deltaTime, _remainingTime);

      while (_accumulatedDeltaTime > _configuration.Delta || Mathf.Approximately(_accumulatedDeltaTime, _configuration.Delta)) {
        _action(_configuration.Delta);
        _accumulatedDeltaTime -= _configuration.Delta;
        _remainingTime = Math.Max(0, _remainingTime - _configuration.Delta);
      }
    }

  }

}