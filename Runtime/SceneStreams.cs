using System.Collections.Generic;
using StreamsForUnity.Exceptions;
using StreamsForUnity.Internal;
using StreamsForUnity.StreamHolders;
using StreamsForUnity.StreamHolders.MonoStreamHolders;
using UnityEngine;
using UnityEngine.SceneManagement;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace StreamsForUnity {

  public static class SceneStreams {

    private static readonly Dictionary<Scene, SceneStreamsHolder> _streamsHolders = new();
    private static readonly MonoStreamHolderFactory _streamHolderFactory = new();

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
      if (!scene.IsValid())
        throw new StreamsException($"Cannot get stream from invalid scene ({scene})");
      return TryGetStream<TSystem>(scene, out ExecutionStream stream)
        ? stream
        : CreateNewStream<TSystem>(scene);
    }

    /// <summary>
    /// Creates the new <see cref="StreamHolder{TSystem}">stream holder</see> and return its stream
    /// </summary>
    public static ExecutionStream CreateNestedStream<THolder>(this Scene scene, string holderName = "StreamHolder")
      where THolder : MonoBehaviour, IStreamHolder {
      return _streamHolderFactory.Create<THolder>(scene, holderName).Stream;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Initialize() {
      SceneManager.sceneUnloaded += DisposeAttachedStreamsOnSceneUnloaded;
      SceneManager.activeSceneChanged += ReorderStreams;
    }

    private static void DisposeAttachedStreamsOnSceneUnloaded(Scene scene) {
      if (!_streamsHolders.TryGetValue(scene, out SceneStreamsHolder holder))
        return;

      holder.DisposeAttachedStreams();
      _streamsHolders.Remove(scene);
    }

    private static void ReorderStreams(Scene current, Scene next) {
      if (current.buildIndex == -1) // on startup the current scene index is -1
        return;

      if (_streamsHolders.TryGetValue(current, out SceneStreamsHolder firstHolder))
        firstHolder.ReorderStreams(uint.MaxValue);
      if (_streamsHolders.TryGetValue(next, out SceneStreamsHolder secondHolder))
        secondHolder.ReorderStreams(0);
    }

#if UNITY_EDITOR
    private static void OnPlayModeStateChange(PlayModeStateChange state) {
      if (state != PlayModeStateChange.ExitingPlayMode)
        return;

      foreach (SceneStreamsHolder holder in _streamsHolders.Values)
        holder.DisposeAttachedStreams();
      _streamsHolders.Clear();

      SceneManager.sceneUnloaded -= DisposeAttachedStreamsOnSceneUnloaded;
      SceneManager.activeSceneChanged -= ReorderStreams;
    }
#endif

    private static bool TryGetStream<TSystem>(Scene scene, out ExecutionStream executionStream) {
      if (_streamsHolders.TryGetValue(scene, out SceneStreamsHolder holder)) {
        if (holder.TryGetStream<TSystem>(out ExecutionStream stream)) {
          executionStream = stream;
          return true;
        }
      }

      executionStream = null;
      return false;
    }

    private static ExecutionStream CreateNewStream<TSystem>(Scene scene) {
      if (!_streamsHolders.ContainsKey(scene))
        _streamsHolders.Add(scene, new SceneStreamsHolder(scene));
      return _streamsHolders[scene].CreateStream<TSystem>();
    }

  }

}