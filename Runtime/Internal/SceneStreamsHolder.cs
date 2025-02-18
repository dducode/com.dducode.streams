using System;
using System.Collections.Generic;
using StreamsForUnity.StreamHolders;

namespace StreamsForUnity.Internal {

  internal sealed class SceneStreamsHolder {

    private readonly Dictionary<Type, IStreamHolder> _holders = new();
    private readonly StreamTokenSource _disposeHandle = new();

    internal void AddStreamHolder<TBaseSystem>(StreamHolder<TBaseSystem> holder, StreamTokenSource disposeHandle) {
      _holders.Add(typeof(TBaseSystem), holder);
      _disposeHandle.Register(disposeHandle.Release);
    }

    internal bool TryGetStream<TBaseSystem>(out ExecutionStream executionStream) {
      if (_holders.TryGetValue(typeof(TBaseSystem), out IStreamHolder runner)) {
        executionStream = runner.Stream;
        return true;
      }

      executionStream = null;
      return false;
    }

    internal void ReorderHolders(uint priority) {
      foreach (IStreamHolder holder in _holders.Values)
        holder.Priority = priority;
    }

    internal void DisposeAttachedHolders() {
      _disposeHandle.Release();
      _holders.Clear();
    }

  }

}