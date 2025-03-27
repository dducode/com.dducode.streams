using System;
using Streams.StreamActions.Components;

namespace Streams.StreamActions {

  public sealed class OnceStreamAction : StreamAction, ICompletable {

    private protected override Delegate Action => _action;

    private readonly Action _action;
    private readonly Completion _completion = new();

    internal OnceStreamAction(Action action, StreamToken cancellationToken, uint priority) : base(cancellationToken, priority) {
      _action = action;
    }

    public void OnComplete(Action onComplete, StreamToken subscriptionToken = default) {
      _completion.OnComplete(onComplete, subscriptionToken);
    }

    internal override void Invoke(float deltaTime) {
      if (Canceled())
        return;

      _action();
      _completion.Complete();
    }

  }

}