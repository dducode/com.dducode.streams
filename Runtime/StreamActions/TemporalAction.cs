using System;
using Streams.StreamActions.Components;
using UnityEngine;

namespace Streams.StreamActions {

  public sealed class TemporalAction : SelfClosingAction, IConfigurable<TemporalAction>, ICompletable {

    public override float DeltaTime => _configuration.HasDelta ? _configuration.Delta : _accumulatedDeltaTime;

    private readonly Configuration _configuration = new();
    private readonly Completion _completion = new();

    private float _remainingTime;
    private ulong _ticks;
    private float _accumulatedDeltaTime;

    internal TemporalAction(Action<SelfClosingAction> action, float time, StreamToken cancellationToken) : base(action, cancellationToken) {
      _remainingTime = time;
    }

    public TemporalAction SetDelta(float value) {
      _configuration.Delta = value;
      _accumulatedDeltaTime = 0;
      return this;
    }

    public TemporalAction ResetDelta() {
      _configuration.ResetDelta();
      _accumulatedDeltaTime = 0;
      return this;
    }

    public TemporalAction SetTickRate(uint value) {
      _configuration.TickRate = value;
      _accumulatedDeltaTime = 0;
      return this;
    }

    public TemporalAction SetPriority(uint value) {
      Priority = value;
      return this;
    }

    public void OnComplete(Action onComplete, StreamToken subscriptionToken = default) {
      _completion.OnComplete(onComplete, subscriptionToken);
    }

    internal override void Invoke(float deltaTime) {
      if (!CanExecute())
        return;

      if (_remainingTime == 0) {
        _completion.Complete();
        return;
      }

      _ticks++;
      _accumulatedDeltaTime += Math.Min(deltaTime, _remainingTime);

      if (!_configuration.HasDelta) {
        if (_ticks % _configuration.TickRate == 0) {
          InvokeAction();
          _accumulatedDeltaTime = 0;
        }

        _remainingTime = Math.Max(0, _remainingTime - deltaTime);
        return;
      }

      while (_accumulatedDeltaTime > _configuration.Delta || Mathf.Approximately(_accumulatedDeltaTime, _configuration.Delta)) {
        InvokeAction();
        _accumulatedDeltaTime -= _configuration.Delta;
        _remainingTime = Math.Max(0, _remainingTime - _configuration.Delta);
      }
    }

  }

}