using System;
using System.Collections;

namespace Streams.StreamActions {

  public class CoroutineStreamAction : StreamAction {

    private protected override Delegate Action => _action;

    private readonly Func<IEnumerator> _action;
    private IEnumerator _coroutine;

    internal CoroutineStreamAction(Func<IEnumerator> action, StreamToken cancellationToken, uint priority) :
      base(cancellationToken, priority) {
      _action = action;
    }

    internal override void Invoke(float deltaTime) {
      if (Canceled())
        return;

      _coroutine ??= _action();

      if (!_coroutine.MoveNext())
        _coroutine = null;
    }

  }

}