using UnityEngine;

namespace StreamsForUnity.Extensions {

  public static class GameObjectExtensions {

    /// <summary>
    /// Searches stream up the hierarchy. If the stream isn't found in hierarchy - returns the scene stream
    /// </summary>
    public static ExecutionStream GetStreamInHierarchy<TSystem>(this GameObject gameObject) {
      return gameObject.transform.GetStreamInHierarchy<TSystem>();
    }

  }

}