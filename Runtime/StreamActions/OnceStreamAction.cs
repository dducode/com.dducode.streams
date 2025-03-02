using System;

namespace StreamsForUnity.StreamActions {

  public sealed class OnceStreamAction : StreamAction, ICompletable {

    public event Action OnComplete {
      add => _completion.OnComplete += value;
      remove => _completion.OnComplete -= value;
    }

    private protected override Delegate Action => _action;

    private readonly Action _action;
    private readonly Completion _completion = new();

    internal OnceStreamAction(Action action, StreamToken cancellationToken, uint priority) : base(cancellationToken, priority) {
      _action = action;
    }

    internal override void Invoke(float deltaTime) {
      if (Canceled())
        return;

      _action();
      _completion.Complete();
    }

  }

}