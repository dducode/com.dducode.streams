using UnityEngine;

namespace Streams.Extensions {

  public static class ComponentExtensions {

    internal static bool TryGetComponentInChildren<T>(this Component me, out T component) {
      var target = me.GetComponentInChildren<T>();

      if (target != null) {
        component = target;
        return true;
      }

      component = default;
      return false;
    }

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