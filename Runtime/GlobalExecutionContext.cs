using System;
using System.Collections.Generic;
using System.Threading;
using Streams.Internal;
using Streams.StreamContexts;
using UnityEngine.LowLevel;
using UnityEngine.PlayerLoop;

namespace Streams {

  public class GlobalExecutionContext : IStreamExecutionContext {

    private readonly Dictionary<Type, ExecutionStream> _connectedStreams = new();
    private readonly RegisteredSystem _systemsTree = new(null, null);
    private readonly CancellationTokenSource _streamsCancellation = new();

    internal GlobalExecutionContext(CancellationToken disposeToken) {
      disposeToken.Register(_streamsCancellation.Cancel);
      _streamsCancellation.Token.Register(() => {
        _connectedStreams.Clear();
        UnregisterSystems();
      });

      CreateStream(typeof(Update));
      CreateStream(typeof(FixedUpdate));
      CreateStream(typeof(PreLateUpdate));
    }

    public ExecutionStream GetStream<TSystem>() {
      return GetStream(typeof(TSystem));
    }

    public ExecutionStream GetStream(Type systemType) {
      return _connectedStreams.TryGetValue(systemType, out ExecutionStream stream) ? stream : CreateStream(systemType);
    }

    private ExecutionStream CreateStream(Type systemType) {
      var stream = new ExecutionStream(NamesUtility.CreateProfilerSampleName(systemType));
      _streamsCancellation.Token.Register(stream.Terminate);

      _connectedStreams.Add(systemType, stream);
      RegisterSystemsInTree(systemType, typeof(StreamRunner));
      var runner = new StreamRunner(stream, SystemIdentifier.IsFixedSystem(systemType));
      var newSystem = new PlayerLoopSystem {
        type = typeof(StreamRunner),
        updateDelegate = runner.Run
      };
      SystemManager.SetupSystem(systemType, newSystem);

      ExecutionContexts.All.Add(stream, this);
      stream.OnTerminate(() => {
        _connectedStreams.Remove(systemType);
        ExecutionContexts.All.Remove(stream);
      });
      return stream;
    }

    private void RegisterSystemsInTree(Type baseSystemType, Type systemType) {
      RegisteredSystem baseSystem;

      if (_systemsTree.TryGetRegisteredSystem(baseSystemType, out RegisteredSystem system)) {
        baseSystem = system;
      }
      else {
        baseSystem = new RegisteredSystem(_systemsTree, baseSystemType);
        _systemsTree.SubSystems.Add(baseSystemType, baseSystem);
      }

      baseSystem.SubSystems.Add(systemType, new RegisteredSystem(baseSystem, systemType));
    }

    private void UnregisterSystems() {
      UnregisterSystem(_systemsTree);
      _systemsTree.Clear();
    }

    private static void UnregisterSystem(RegisteredSystem system) {
      foreach (RegisteredSystem subSystem in system.SubSystems.Values)
        UnregisterSystem(subSystem);
      if (system.BaseSystem is { SystemType: not null })
        SystemManager.RemoveSystem(system.BaseSystem.SystemType, system.SystemType);
    }

  }

}