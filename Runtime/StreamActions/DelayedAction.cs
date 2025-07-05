using System;
using Streams.StreamActions.Components;

namespace Streams.StreamActions {

  internal sealed class DelayedAction : StreamActionBase, ICompletable {

    private readonly Action _action;
    private readonly Completion _completion = new();
    private float _remainingTime;

    internal DelayedAction(float time, Action action, StreamToken cancellationToken) : base(cancellationToken) {
      _action = action;
      _remainingTime = time;
    }

    public void OnComplete(Action onComplete, StreamToken subscriptionToken = default) {
      _completion.OnComplete(onComplete, subscriptionToken);
    }

    public override bool Invoke(float deltaTime) {
      if (!base.Invoke(deltaTime))
        return false;

      _remainingTime = Math.Max(0, _remainingTime - deltaTime);

      if (_remainingTime > 0)
        return true;

      _action();
      _completion.Complete();
      return false;
    }

  }

}