using System;
using Streams.StreamActions.Components;

namespace Streams.StreamActions {

  public sealed class OnceAction : StreamActionBase, ICompletable {

    public bool IsCompleted => _completion.IsCompleted;

    private protected override Delegate Action => _action;

    private readonly Action _action;
    private readonly Completion _completion = new();

    internal OnceAction(Action action, StreamToken cancellationToken) : base(cancellationToken) {
      _action = action;
    }

    public void OnComplete(Action onComplete, StreamToken subscriptionToken = default) {
      _completion.OnComplete(onComplete, subscriptionToken);
    }

    internal override void Invoke(float deltaTime) {
      base.Invoke(deltaTime);
      _action();
      _completion.Complete();
    }

  }

}