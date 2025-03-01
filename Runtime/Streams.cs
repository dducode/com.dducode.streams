using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using StreamsForUnity.Exceptions;
using StreamsForUnity.Internal;
using UnityEngine;
using UnityEngine.PlayerLoop;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace StreamsForUnity {

  public static class Streams {

    /// <summary>
    /// Gets the currently running stream. If you try to get this outside of any stream, you'll get null
    /// </summary>
    [CanBeNull]
    public static ExecutionStream RunningStream => _streamsStack.Count == 0 ? null : _streamsStack.Peek();

    private static readonly Dictionary<Type, ExecutionStream> _connectedStreams = new();
    private static readonly Stack<ExecutionStream> _streamsStack = new();
    private static StreamTokenSource _streamsCancellation = new();

#if UNITY_EDITOR
    static Streams() {
      EditorApplication.playModeStateChanged += state => {
        if (state == PlayModeStateChange.ExitingPlayMode)
          DisposeAllStreams();
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
    public static ExecutionStream Get<TSystem>() {
#if UNITY_EDITOR
      if (!EditorApplication.isPlaying)
        throw new StreamsException("Cannot get stream when editor is not playing");
#endif
      return _connectedStreams.TryGetValue(typeof(TSystem), out ExecutionStream stream) ? stream : CreateStream<TSystem>();
    }

    internal static void PushStream(ExecutionStream stream) {
      _streamsStack.Push(stream);
    }

    internal static void PopStream() {
      _streamsStack.Pop();
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Initialize() {
      _streamsCancellation = new StreamTokenSource();

      CreateStream<Update>();
      CreateStream<FixedUpdate>();
      CreateStream<PreLateUpdate>();
    }

    private static ExecutionStream CreateStream<TSystem>() {
      var stream = new ExecutionStream(NamesUtility.CreateProfilerSampleName(typeof(TSystem)));
      _streamsCancellation.Register(stream.Terminate);

      Type systemType = typeof(TSystem);
      _connectedStreams.Add(systemType, stream);
      StreamConnector.Connect<TSystem>(stream);

      stream.OnDispose(() => {
        _connectedStreams.Remove(systemType);
        StreamConnector.DisconnectStreamAt<TSystem>();
      });
      return stream;
    }

    private static void DisposeAllStreams() {
      _streamsCancellation.Release();
      _connectedStreams.Clear();
    }

  }

}