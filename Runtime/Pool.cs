using System;
using System.Collections.Generic;
using JetBrains.Annotations;

namespace Streams {

  internal static class Pool {

    private static readonly Dictionary<Type, Stack<object>> _pool = new();

    [Pure]
    internal static TConcrete Get<TConcrete>() where TConcrete : new() {
      lock (_pool)
        if (_pool.TryGetValue(typeof(TConcrete), out Stack<object> stack))
          if (stack.TryPop(out object result))
            return (TConcrete)result;

      return new TConcrete();
    }

    internal static void Return([NotNull] object instance) {
      if (instance == null)
        throw new ArgumentNullException(nameof(instance));

      Type instanceType = instance.GetType();
      lock (_pool) {
        if (!_pool.ContainsKey(instanceType))
          _pool.Add(instanceType, new Stack<object>());
        _pool[instanceType].Push(instance);
      }
    }

  }

}