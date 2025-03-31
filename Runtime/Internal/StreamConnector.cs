using System;
using UnityEngine;
using UnityEngine.LowLevel;

namespace Streams.Internal {

  internal static class StreamConnector {

    internal static void Connect(ExecutionStream stream, Type systemType) {
      Func<float> deltaTime = SystemIdentifier.IsFixedSystem(systemType) ? () => Time.fixedDeltaTime : () => Time.deltaTime;
      var newSystem = new PlayerLoopSystem {
        type = typeof(ExecutionStream),
        updateDelegate = () => stream.Update(deltaTime())
      };
      SystemManager.SetupSystem(systemType, newSystem);
    }

  }

}