using StreamsForUnity.StreamHolders;
using UnityEngine;

namespace StreamsForUnity.Extensions {

  public static class GameObjectExtensions {

    public static ExecutionStream GetStream<TSystem>(this GameObject gameObject) {
      GameObjectStreamsHolder holder = gameObject.GetComponent<GameObjectStreamsHolder>() ?? gameObject.AddComponent<GameObjectStreamsHolder>();
      return holder.GetStream<TSystem>();
    }

  }

}