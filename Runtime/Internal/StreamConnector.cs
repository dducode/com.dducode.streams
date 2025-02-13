using System;
using UnityEngine;

namespace StreamsForUnity.Internal {

  internal static class StreamConnector {

    internal static void Connect<TSystem>(ExecutionStream stream) {
      Type systemType = typeof(TSystem);
      Func<float> deltaTime = SystemIdentifier.IsFixedSystem(systemType) ? () => Time.fixedDeltaTime : () => Time.deltaTime;
      SystemRegistrar.RegisterStreamAsSystem<TSystem>(() => stream.Update(deltaTime()));
      stream.OnDispose(SystemRegistrar.UnregisterStream<TSystem>);
    }

  }

}