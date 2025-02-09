using System;

namespace StreamsForUnity.Internal {

  internal static class StreamConnector {

    internal static void Connect<TSystem>(ExecutionStream stream) {
      Type systemType = typeof(TSystem);
      Func<float> deltaTime = RuntimeCompileUtility.CreateDeltaTimeProperty(SystemIdentifier.IsFixedSystem(systemType));
      SystemRegistrar.RegisterStreamAsSystem<TSystem>(() => stream.Update(deltaTime()));
      stream.OnDispose(SystemRegistrar.UnregisterStream<TSystem>);
    }

  }

}