using System;
using System.Collections.Generic;
using UnityEngine;

namespace Streams.StreamActions {

  internal sealed class ContinuationsHandler : StreamActionBase, IDisposable {

    private readonly Queue<Action> _continuations = new();
    private readonly object _lock = new();

    internal ContinuationsHandler(StreamToken cancellationToken) : base(cancellationToken) {
    }

    internal void Enqueue(Action continuation) {
      lock (_lock)
        _continuations.Enqueue(continuation);
    }

    internal void CopyFrom(ContinuationsHandler other) {
      foreach (Action continuation in other._continuations)
        lock (_lock)
          _continuations.Enqueue(continuation);
    }

    public override bool Invoke(float deltaTime) {
      lock (_lock) {
        while (_continuations.TryDequeue(out Action continuation)) {
          try {
            continuation();
          }
          catch (Exception e) {
            Debug.LogError($"An error occurred while executing continuation of declaring type <b>{continuation.Target.GetType()}</b>");
            Debug.LogException(e);
          }
        }
      }

      return true;
    }

    public void Dispose() {
      _continuations.Clear();
    }

  }

}