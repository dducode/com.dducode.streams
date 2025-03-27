using System;
using UnityEngine.LowLevel;

namespace Streams.Internal {

  internal static class SystemRegistrar {

    internal static void RegisterStreamAsSystem(PlayerLoopSystem.UpdateFunction updateFunction, Type systemType) {
      var newSystem = new PlayerLoopSystem {
        type = typeof(ExecutionStream),
        updateDelegate = updateFunction
      };
      SystemManager.SetupSystem(newSystem, systemType);
    }

    internal static void UnregisterStream(Type systemType) {
      SystemManager.RemoveSystem(systemType, typeof(ExecutionStream));
    }

  }

}