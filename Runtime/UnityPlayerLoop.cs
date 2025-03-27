using System;
using System.Collections.Generic;
using Streams.Exceptions;
using Streams.Internal;
using UnityEditor;
using UnityEngine;
using UnityEngine.PlayerLoop;

namespace Streams {

  public static class UnityPlayerLoop {

    private static readonly Dictionary<Type, ExecutionStream> _connectedStreams = new();
    private static StreamTokenSource _streamsCancellation = new();

#if UNITY_EDITOR
    static UnityPlayerLoop() {
      EditorApplication.playModeStateChanged += state => {
        if (state == PlayModeStateChange.ExitingPlayMode) {
          _streamsCancellation.Release();
          _connectedStreams.Clear();
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
      return GetStream(typeof(TSystem));
    }

    internal static ExecutionStream GetStream(Type systemType) {
      return _connectedStreams.TryGetValue(systemType, out ExecutionStream stream) ? stream : CreateStream(systemType);
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Initialize() {
      _streamsCancellation = new StreamTokenSource();

      CreateStream(typeof(Update));
      CreateStream(typeof(FixedUpdate));
      CreateStream(typeof(PreLateUpdate));
    }

    private static ExecutionStream CreateStream(Type systemType) {
      var stream = new ExecutionStream(NamesUtility.CreateProfilerSampleName(systemType));
      _streamsCancellation.Register(stream.Terminate);

      _connectedStreams.Add(systemType, stream);
      StreamConnector.Connect(stream, systemType);

      stream.OnTerminate(() => {
        _connectedStreams.Remove(systemType);
        StreamConnector.DisconnectStreamAt(systemType);
      });
      return stream;
    }

  }

}