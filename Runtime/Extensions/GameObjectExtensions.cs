using Streams.StreamHolders;
using UnityEngine;

namespace Streams.Extensions {

  public static class GameObjectExtensions {

    public static ExecutionStream GetStream<TSystem>(this GameObject gameObject) {
      GameObjectStreamsHolder holder = gameObject.GetComponent<GameObjectStreamsHolder>() ?? gameObject.AddComponent<GameObjectStreamsHolder>();
      return holder.GetStream<TSystem>();
    }

  }

}