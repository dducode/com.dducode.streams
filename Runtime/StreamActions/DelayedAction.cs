using System;
using Streams.StreamActions.Components;

namespace Streams.StreamActions {

  public sealed class DelayedAction : StreamActionBase, ICompletable {

    public bool IsCompleted => _completion.IsCompleted;

    private protected override Delegate Action => _action;

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

    internal override void Invoke(float deltaTime) {
      base.Invoke(deltaTime);
      _remainingTime = Math.Max(0, _remainingTime - deltaTime);

      if (_remainingTime > 0)
        return;

      _action();
      _completion.Complete();
    }

  }

}