using UnityEngine.LowLevel;

namespace StreamsForUnity.Internal {

  internal static class SystemRegistrar {

    internal static void RegisterStreamAsSystem<TBaseSystem>(PlayerLoopSystem.UpdateFunction updateFunction) {
      var newSystem = new PlayerLoopSystem {
        type = typeof(ExecutionStream),
        updateDelegate = updateFunction
      };
      SystemManager.SetupSystem<TBaseSystem>(newSystem);
    }

    internal static void UnregisterStream<TBaseSystem>() {
      SystemManager.RemoveSystem<TBaseSystem, ExecutionStream>();
    }

  }

}