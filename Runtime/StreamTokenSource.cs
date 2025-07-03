using System;
using System.Collections.Generic;
using JetBrains.Annotations;

namespace Streams {

  public class StreamTokenSource : IDisposable {

    public StreamToken Token { get; }
    public bool Released { get; private set; }

    private readonly Queue<Action> _onReleaseActions = new();

    public StreamTokenSource() {
      Token = new StreamToken(this);
    }

    public void Release() {
      if (Released)
        return;
      Released = true;

      while (_onReleaseActions.TryDequeue(out Action action))
        action();
    }

    internal void Register([NotNull] Action onReleaseAction) {
      if (Released) {
        onReleaseAction();
        return;
      }

      _onReleaseActions.Enqueue(onReleaseAction);
    }

    public void Dispose() {
      Release();
    }

  }

}