using System;
using System.Collections.Generic;
using JetBrains.Annotations;

namespace StreamsForUnity {

  public class StreamTokenSource {

    public StreamToken Token { get; }
    public bool Released { get; private set; }

    private readonly Queue<Action> _onReleaseActions = new();

    public StreamTokenSource() {
      Token = new StreamToken(this);
    }

    public void Release() {
      if (Released)
        return;
      while (_onReleaseActions.TryDequeue(out Action action))
        action();
      Released = true;
    }

    internal void Register([NotNull] Action onReleaseAction) {
      if (Released)
        throw new StreamsException("Cannot register action after the token source has been released");
      if (onReleaseAction == null)
        throw new ArgumentNullException(nameof(onReleaseAction));
      _onReleaseActions.Enqueue(onReleaseAction);
    }

  }

}