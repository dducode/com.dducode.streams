using System;
using System.Threading;
using Streams.StreamActions.Components;

namespace Streams.StreamActions {

  public sealed class StreamTimer : StreamAction, ICompletable {

    private protected override Delegate Action => _action;

    private readonly Action _action;
    private readonly Completion _completion = new();
    private float _remainingTime;

    internal StreamTimer(float time, Action action, CancellationToken cancellationToken) : base(cancellationToken) {
      _action = action;
      _remainingTime = time;
    }

    public void OnComplete(Action onComplete, CancellationToken subscriptionToken = default) {
      _completion.OnComplete(onComplete, subscriptionToken);
    }

    internal override void Invoke(float deltaTime) {
      if (Canceled())
        return;

      _remainingTime = Math.Max(0, _remainingTime - deltaTime);

      if (_remainingTime == 0) {
        _action();
        _completion.Complete();
      }
    }

  }

}