using System;
using System.Collections.Generic;
using System.Threading;

namespace StreamsForUnity.Internal {

  internal class SceneStreamsHolder {

    private readonly Dictionary<Type, ExecutionStream> _streams = new();
    private readonly Dictionary<ExecutionStream, ExecutionStream> _streamsToBaseStreamsMap = new();
    private readonly Dictionary<ExecutionStream, CancellationTokenSource> _disposeHandles = new();
    private readonly Dictionary<ExecutionStream, CancellationTokenSource> _subscriptionHandles = new();

    internal void AddStream<TBaseSystem>(ExecutionStream baseStream, ExecutionStream stream, CancellationTokenSource disposeHandle, uint priority) {
      _streams.Add(typeof(TBaseSystem), stream);
      _streamsToBaseStreamsMap.Add(stream, baseStream);

      var subscriptionHandle = new CancellationTokenSource();
      baseStream.Add(stream.Update, subscriptionHandle.Token, priority);
      _subscriptionHandles.Add(stream, subscriptionHandle);

      stream.OnDispose(() => _streams.Remove(typeof(TBaseSystem)));
      _disposeHandles.Add(stream, disposeHandle);
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
      foreach ((ExecutionStream stream, ExecutionStream baseStream) in _streamsToBaseStreamsMap) {
        _subscriptionHandles[stream].Cancel();
        var subscriptionHandle = new CancellationTokenSource();
        baseStream.Add(stream.Update, subscriptionHandle.Token, priority);
        _subscriptionHandles[stream] = subscriptionHandle;
      }
    }

    internal void DisposeAttachedStreams() {
      foreach (ExecutionStream stream in _streams.Values)
        _disposeHandles[stream].Cancel();
    }

  }

}