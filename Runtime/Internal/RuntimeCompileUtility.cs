using System;
using System.Linq.Expressions;
using System.Reflection;
using UnityEngine;

namespace StreamsForUnity.Internal {

  internal static class RuntimeCompileUtility {

    internal static Func<float> CreateDeltaTimeProperty(bool isFixed) {
      PropertyInfo propertyInfo = typeof(Time).GetProperty(isFixed ? nameof(Time.fixedDeltaTime) : nameof(Time.deltaTime));
      MemberExpression propertyAccessor = Expression.Property(null, propertyInfo ?? throw new InvalidOperationException());
      return Expression.Lambda<Func<float>>(propertyAccessor).Compile();
    }

  }

}