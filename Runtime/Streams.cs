using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using StreamsForUnity.Internal;
using UnityEngine;
using UnityEngine.PlayerLoop;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace StreamsForUnity {

  public static class Streams {

    [CanBeNull] public static ExecutionStream RunningStream => _streamsStack.Count == 0 ? null : _streamsStack.Peek();

    private static readonly Dictionary<Type, ExecutionStream> _connectedStreams = new();
    private static readonly Stack<ExecutionStream> _streamsStack = new();
    private static StreamTokenSource _streamsCancellation = new();

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
      Application.quitting += DisposeAllStreams;

      CreateStream<Update>();
      CreateStream<FixedUpdate>();
      CreateStream<PreLateUpdate>();
    }

    private static ExecutionStream CreateStream<TSystem>() {
      var stream = new ExecutionStream(_streamsCancellation.Token, NamesUtility.CreateProfilerSampleName(typeof(TSystem)));
      StreamConnector.Connect<TSystem>(stream);
      Type systemType = typeof(TSystem);
      _connectedStreams.Add(systemType, stream);
      stream.OnDispose(() => _connectedStreams.Remove(systemType));
      return stream;
    }

    private static void DisposeAllStreams() {
      _streamsCancellation.Release();
      _connectedStreams.Clear();
      Application.quitting -= DisposeAllStreams;
    }

  }

}