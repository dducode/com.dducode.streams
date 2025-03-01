using System;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

namespace StreamsForUnity.Internal {

  internal sealed class SceneStreamsHolder {

    private readonly Scene _scene;
    private readonly Dictionary<Type, ManagedExecutionStream> _streams = new();
    private readonly StreamTokenSource _disposeHandle = new();

    public SceneStreamsHolder(Scene scene) {
      _scene = scene;
    }

    internal ExecutionStream CreateStream<TSystem>() {
      uint priority = SceneManager.GetActiveScene() == _scene ? 0 : uint.MaxValue;
      var stream = new ManagedExecutionStream(Streams.Get<TSystem>(), _scene.name, priority);
      _streams.Add(typeof(TSystem), stream);
      _disposeHandle.Register(stream.Dispose);
      return stream;
    }

    internal bool TryGetStream<TSystem>(out ExecutionStream executionStream) {
      if (_streams.TryGetValue(typeof(TSystem), out ManagedExecutionStream stream)) {
        executionStream = stream;
        return true;
      }

      executionStream = null;
      return false;
    }

    internal void ReorderStreams(uint priority) {
      foreach (ManagedExecutionStream stream in _streams.Values)
        stream.Priority = priority;
    }

    internal void DisposeAttachedStreams() {
      _disposeHandle.Release();
      _streams.Clear();
    }

  }

}