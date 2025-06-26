using System;
using System.Collections.Generic;
using System.Threading;
using Streams.Exceptions;
using Streams.StreamContexts;
using UnityEditor;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.SceneManagement;

namespace Streams {

  public static class SceneStreams {

    private static readonly Dictionary<Scene, IStreamExecutionContext> _streamsContexts = new();
    private static StreamTokenSource _disposeHandle;

#if UNITY_EDITOR
    static SceneStreams() {
      EditorApplication.playModeStateChanged += OnPlayModeStateChange;
    }
#endif

    /// <summary>
    /// Gets the stream attached to the current scene and running on the specified system
    /// </summary>
    /// <returns> Existing stream or new </returns>
    /// <exception cref="StreamsException"> Threw if the scene is invalid </exception>
    public static ExecutionStream GetStream<TSystem>(this Scene scene) {
      return GetStream(scene, typeof(TSystem));
    }

    internal static ExecutionStream GetStream(this Scene scene, Type systemType) {
      return GetStreamsContext(scene).GetStream(systemType);
    }

    private static IStreamExecutionContext GetStreamsContext(this Scene scene) {
      if (!scene.IsValid())
        throw new StreamsException("Cannot get streams context from invalid scene");

      if (!_streamsContexts.ContainsKey(scene))
        CreateStreamsContextForScene(scene);

      return _streamsContexts[scene];
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Initialize() {
      _disposeHandle = new StreamTokenSource();
    }

    private static void CreateStreamsContextForScene(Scene scene) {
      var context = new SceneExecutionContext(scene, _disposeHandle.Token);
      _streamsContexts.Add(scene, context);
      context.OnDispose(() => _streamsContexts.Remove(scene));
    }

#if UNITY_EDITOR
    private static void OnPlayModeStateChange(PlayModeStateChange state) {
      if (state != PlayModeStateChange.ExitingPlayMode)
        return;

      _disposeHandle.Release();
      Assert.IsTrue(_streamsContexts.Count == 0, "Internal error - not all contexts were released");
    }
#endif

  }

}