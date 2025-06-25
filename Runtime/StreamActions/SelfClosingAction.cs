using System;
using System.Threading;

namespace Streams.StreamActions {

  public abstract class SelfClosingAction<TAction> : StreamAction where TAction : SelfClosingAction<TAction> {

    public abstract float DeltaTime { get; }
    private protected override Delegate Action => _action;
    private readonly Action<TAction> _action;

    protected SelfClosingAction(Action<TAction> action, StreamToken cancellationToken) : base(cancellationToken) {
      _action = action;
    }

    private protected void InvokeAction() {
      _action((TAction)this);
    }

  }

  public abstract class SelfClosingAction<TAction, TReturn> : StreamAction where TAction : SelfClosingAction<TAction, TReturn> {

    public abstract float DeltaTime { get; }

    private protected override Delegate Action => _func;
    private readonly Func<TAction, TReturn> _func;

    protected SelfClosingAction(Func<TAction, TReturn> func, StreamToken cancellationToken) : base(cancellationToken) {
      _func = func;
    }

    private protected TReturn InvokeAction() {
      return _func((TAction)this);
    }

  }

}