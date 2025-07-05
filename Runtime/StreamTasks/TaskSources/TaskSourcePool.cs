using System;
using System.Collections.Generic;

namespace Streams.StreamTasks.TaskSources {

  internal static class TaskSourcePool {

    private static readonly Dictionary<Type, Stack<IStreamTaskSource>> _pool = new();

    internal static TSource Get<TSource>() where TSource : class, IStreamTaskSource, new() {
      lock (_pool)
        if (_pool.TryGetValue(typeof(TSource), out Stack<IStreamTaskSource> stack))
          if (stack.TryPop(out IStreamTaskSource source))
            return (TSource)source;

      return new TSource();
    }

    internal static void Return(IStreamTaskSource source) {
      Type sourceType = source.GetType();
      lock (_pool) {
        if (!_pool.ContainsKey(sourceType))
          _pool.Add(sourceType, new Stack<IStreamTaskSource>());
        _pool[sourceType].Push(source);
      }
    }

  }

}