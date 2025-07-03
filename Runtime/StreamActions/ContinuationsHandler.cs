using System;
using System.Collections.Generic;
using UnityEngine;

namespace Streams.StreamActions {

  internal sealed class ContinuationsHandler : StreamActionBase {

    private readonly Queue<Action> _continuations = new();
    private readonly List<(float time, Action continuation)> _delayedContinuations = new();
    private readonly object _lock = new();

    internal ContinuationsHandler(StreamToken cancellationToken) : base(cancellationToken) {
    }

    internal void Enqueue(Action continuation) {
      lock (_lock)
        _continuations.Enqueue(continuation);
    }

    internal void Enqueue(float time, Action continuation) {
      lock (_lock)
        _delayedContinuations.Add((Time.time + time, continuation));
    }

    public override void Invoke(float deltaTime) {
      base.Invoke(deltaTime);

      while (_continuations.TryDequeue(out Action continuation))
        continuation();

      foreach ((float time, Action continuation) pair in _delayedContinuations)
        if (Time.time > pair.time)
          pair.continuation();

      _delayedContinuations.RemoveAll(pair => Time.time > pair.time);
    }

  }

}