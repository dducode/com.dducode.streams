using UnityEngine;

namespace StreamsForUnity.Internal.Extensions {

  internal static class ComponentExtensions {

    internal static bool TryGetComponentInParent<T>(this Component me, out T component) {
      var target = me.GetComponentInParent<T>();

      if (target != null) {
        component = target;
        return true;
      }

      component = default;
      return false;
    }

  }

}