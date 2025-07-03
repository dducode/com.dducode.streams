using System;
using System.Collections.Generic;
using UnityEngine;

namespace Streams.StreamTasks.TaskSources {

  internal static class TaskSourcePool {

    private static readonly Dictionary<Type, Queue<IStreamTaskSource>> _pool = new();

    internal static bool TryGet<TSource>(out TSource source) where TSource : class, IStreamTaskSource {
      if (_pool.TryGetValue(typeof(TSource), out Queue<IStreamTaskSource> queue)) {
        if (queue.TryDequeue(out IStreamTaskSource result)) {
          source = (TSource)result;
          source.Reset();
          return true;
        }
      }

      source = null;
      return false;
    }

    internal static void Return(IStreamTaskSource source) {
      Type sourceType = source.GetType();
      if (!_pool.ContainsKey(sourceType))
        _pool.Add(sourceType, new Queue<IStreamTaskSource>());
      _pool[sourceType].Enqueue(source);
    }

    [RuntimeInitializeOnLoadMethod]
    private static void Initialize() {
      _pool.Clear();
    }

  }

}