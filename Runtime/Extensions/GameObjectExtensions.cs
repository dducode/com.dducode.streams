using Streams.StreamContexts;
using UnityEngine;

namespace Streams.Extensions {

  public static class GameObjectExtensions {

    public static ExecutionStream GetStream<TSystem>(this GameObject gameObject) {
      GameObjectExecutionContext context = gameObject.GetComponent<GameObjectExecutionContext>() ?? gameObject.AddComponent<GameObjectExecutionContext>();
      return context.GetStream<TSystem>();
    }

  }

}