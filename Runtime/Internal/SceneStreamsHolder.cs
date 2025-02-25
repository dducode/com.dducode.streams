using System;
using System.Collections.Generic;

namespace StreamsForUnity.Internal {

  internal sealed class SceneStreamsHolder {

    private readonly Dictionary<Type, ExecutionStream> _streams = new();
    private readonly Dictionary<ExecutionStream, StreamAction> _executions = new();
    private readonly StreamTokenSource _disposeHandle = new();

    internal void AddStream<TBaseSystem>(ExecutionStream stream, StreamTokenSource disposeHandle, uint priority) {
      _streams.Add(typeof(TBaseSystem), stream);
      _executions.Add(stream, Streams.Get<TBaseSystem>().Add(stream.Update, disposeHandle.Token, priority));
      _disposeHandle.Register(disposeHandle.Release);
    }

    internal bool TryGetStream<TBaseSystem>(out ExecutionStream executionStream) {
      if (_streams.TryGetValue(typeof(TBaseSystem), out ExecutionStream stream)) {
        executionStream = stream;
        return true;
      }

      executionStream = null;
      return false;
    }

    internal void ReorderStreams(uint priority) {
      foreach (ExecutionStream stream in _streams.Values)
        _executions[stream].ChangePriority(priority);
    }

    internal void DisposeAttachedHolders() {
      _disposeHandle.Release();
      _streams.Clear();
    }

  }

}