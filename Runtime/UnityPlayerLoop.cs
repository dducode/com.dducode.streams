using System;
using System.Collections.Generic;
using Streams.Exceptions;
using Streams.Internal;
using UnityEditor;
using UnityEngine;
using UnityEngine.LowLevel;
using UnityEngine.PlayerLoop;

namespace Streams {

  public static class UnityPlayerLoop {

    private static readonly Dictionary<Type, ExecutionStream> _connectedStreams = new();
    private static readonly RegisteredSystem _systemsTree = new(null, null);
    private static StreamTokenSource _streamsCancellation = new();
    private static bool _initialized;

#if UNITY_EDITOR
    static UnityPlayerLoop() {
      EditorApplication.playModeStateChanged += state => {
        if (state == PlayModeStateChange.ExitingPlayMode) {
          _streamsCancellation.Release();
          _connectedStreams.Clear();
          UnregisterSystems();
          _initialized = false;
        }
      };
    }
#endif

    /// <summary>
    /// Gets the base stream in the specified player loop system
    /// </summary>
    /// <typeparam name="TSystem"> The system that contains the stream </typeparam>
    /// <returns> Existing stream or new </returns>
    /// <exception cref="StreamsException"> (Editor only) Threw when trying to get a stream when the editor isn't playing </exception>
    /// <remarks> Three streams are always created at system startup (in the <see cref="Update"/>, <see cref="FixedUpdate"/> and <see cref="PreLateUpdate"/> systems) </remarks>
    public static ExecutionStream GetStream<TSystem>() {
#if UNITY_EDITOR
      if (!EditorApplication.isPlaying)
        throw new StreamsException("Cannot get stream when editor is not playing");
#endif
      if (!_initialized)
        Initialize();

      return GetStream(typeof(TSystem));
    }

    internal static ExecutionStream GetStream(Type systemType) {
      return _connectedStreams.TryGetValue(systemType, out ExecutionStream stream) ? stream : CreateStream(systemType);
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Initialize() {
      if (_initialized)
        return;

      _streamsCancellation = new StreamTokenSource();
      CreateStream(typeof(Update));
      CreateStream(typeof(FixedUpdate));
      CreateStream(typeof(PreLateUpdate));
      _initialized = true;
    }

    private static ExecutionStream CreateStream(Type systemType) {
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

      stream.OnTerminate(() => _connectedStreams.Remove(systemType));
      return stream;
    }

    private static void RegisterSystemsInTree(Type baseSystemType, Type systemType) {
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

    private static void UnregisterSystems() {
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