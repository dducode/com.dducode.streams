using System;
using System.Collections.Generic;
using StreamsForUnity.Exceptions;
using StreamsForUnity.StreamHolders;
using UnityEngine;
using UnityEngine.SceneManagement;

#if UNITY_EDITOR
using UnityEditor;
using UnityEngine.Assertions;
#endif

namespace StreamsForUnity {

  public static class SceneStreams {

    private static readonly Dictionary<Scene, SceneStreamsHolder> _streamsHolders = new();
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
      return GetStreamsHolder(scene).GetStream(systemType);
    }

    internal static IStreamsHolder GetStreamsHolder(this Scene scene) {
      if (!scene.IsValid())
        throw new StreamsException("Cannot get streams holder from invalid scene");

      if (!_streamsHolders.ContainsKey(scene))
        CreateStreamsHolderForScene(scene);

      return _streamsHolders[scene];
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Initialize() {
      _disposeHandle = new StreamTokenSource();
    }

    private static void CreateStreamsHolderForScene(Scene scene) {
      var holder = new SceneStreamsHolder(scene, _disposeHandle.Token);
      _streamsHolders.Add(scene, holder);
      holder.OnDispose(() => _streamsHolders.Remove(scene));
    }

#if UNITY_EDITOR
    private static void OnPlayModeStateChange(PlayModeStateChange state) {
      if (state != PlayModeStateChange.ExitingPlayMode)
        return;

      _disposeHandle.Release();
      Assert.IsTrue(_streamsHolders.Count == 0, "Internal error - not all holders were released");
    }
#endif

  }

}