using System;
using UnityEngine;

namespace Streams.Internal {

  internal static class StreamConnector {

    internal static void Connect(ExecutionStream stream, Type systemType) {
      Func<float> deltaTime = SystemIdentifier.IsFixedSystem(systemType)
        ? () => Time.fixedDeltaTime
        : () => MathF.Max(Time.deltaTime, float.Epsilon); // deltaTime is zero at the first frame
      SystemRegistrar.RegisterStreamAsSystem(() => stream.Update(deltaTime()), systemType);
    }

    internal static void DisconnectStreamAt(Type systemType) {
      SystemRegistrar.UnregisterStream(systemType);
    }

  }

}