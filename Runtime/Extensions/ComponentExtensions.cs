using StreamsForUnity.StreamHolders;
using UnityEngine;

namespace StreamsForUnity.Extensions {

  public static class ComponentExtensions {

    /// <inheritdoc cref="GameObjectExtensions.GetStreamInHierarchy{TSystem}"/>
    public static ExecutionStream GetStreamInHierarchy<TSystem>(this Component component) {
      return component.TryGetComponentInParent(out StreamHolder<TSystem> holder) ? holder.Stream : component.gameObject.scene.GetStream<TSystem>();
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