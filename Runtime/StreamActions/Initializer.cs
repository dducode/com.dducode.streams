using System;
using System.Collections.Generic;
using UnityEngine;

namespace Streams.StreamActions {

  internal class Initializer : StreamActionBase, IDisposable {

    internal readonly Queue<IInitializable> corruptedObjects = new();

    private readonly Queue<IInitializable> _initializables = new();
    private readonly object _lock = new();

    internal Initializer(StreamToken cancellationToken) : base(cancellationToken) {
    }

    internal void Enqueue(IInitializable initializable) {
      lock (_lock)
        _initializables.Enqueue(initializable);
    }

    internal void CopyFrom(Initializer other) {
      foreach (IInitializable initializable in other._initializables)
        lock (_lock)
          _initializables.Enqueue(initializable);
    }

    public override bool Invoke(float deltaTime) {
      lock (_lock) {
        while (_initializables.TryDequeue(out IInitializable initializable)) {
          try {
            initializable.Initialize();
          }
          catch (Exception e) {
            Debug.LogError($"An error occurred while initialize object with type <b>{initializable.GetType()}</b>");
            Debug.LogException(e);
            corruptedObjects.Enqueue(initializable);
          }
        }
      }

      return true;
    }

    public void Dispose() {
      _initializables.Clear();
    }

  }

}