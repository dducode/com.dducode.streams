using System;
using Streams.StreamActions.Components;

namespace Streams.StreamActions {

  internal sealed class OnceAction : StreamActionBase, ICallbackCompletable {

    public bool IsCompleted => _completion.IsCompleted;

    private readonly Action _action;
    private readonly Completion _completion = new();

    internal OnceAction(Action action, StreamToken cancellationToken) : base(cancellationToken) {
      _action = action;
    }

    public void OnComplete(Action onComplete, StreamToken subscriptionToken = default) {
      _completion.OnComplete(onComplete, subscriptionToken);
    }

    public override void Invoke(float deltaTime) {
      base.Invoke(deltaTime);
      _action();
      _completion.Complete();
    }

  }

}