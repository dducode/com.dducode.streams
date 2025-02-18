using System.Collections.Generic;
using StreamsForUnity.Internal;
using StreamsForUnity.StreamHolders;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace StreamsForUnity {

  public static class SceneStreams {

    private static readonly Dictionary<Scene, SceneStreamsHolder> _streamsHolders = new();
    private static readonly MonoStreamHolderFactory _streamHolderFactory = new();

    public static ExecutionStream GetStream<TBaseSystem>(this Scene scene) {
      if (!scene.IsValid())
        throw new StreamsException($"Cannot get stream from invalid scene ({scene})");
      if (TryGetStream<TBaseSystem>(scene, out ExecutionStream stream))
        return stream;

      var disposeHandle = new StreamTokenSource();
      uint priority = SceneManager.GetActiveScene() == scene ? 0 : uint.MaxValue;
      var runner = new StreamHolder<TBaseSystem>(disposeHandle.Token, scene.name, priority);

      RegisterStreamRunner(scene, runner, disposeHandle);
      return runner.Stream;
    }

    public static ExecutionStream CreateNestedStream<THolder>(this Scene scene, string streamName = "StreamHolder")
      where THolder : MonoBehaviour, IStreamHolder {
      return _streamHolderFactory.Create<THolder>(scene, streamName).Stream;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Initialize() {
      SceneManager.sceneUnloaded += DisposeAttachedStreamsOnSceneUnloaded;
      SceneManager.activeSceneChanged += ReorderStreams;
      Application.quitting += DisposeAllRunners;
    }

    private static void DisposeAttachedStreamsOnSceneUnloaded(Scene scene) {
      if (!_streamsHolders.TryGetValue(scene, out SceneStreamsHolder holder))
        return;

      holder.DisposeAttachedHolders();
      _streamsHolders.Remove(scene);
    }

    private static void ReorderStreams(Scene current, Scene next) {
      if (current.buildIndex == -1) // on startup the current scene index is -1
        return;

      if (_streamsHolders.TryGetValue(current, out SceneStreamsHolder firstHolder))
        firstHolder.ReorderHolders(uint.MaxValue);
      if (_streamsHolders.TryGetValue(next, out SceneStreamsHolder secondHolder))
        secondHolder.ReorderHolders(0);
    }

    private static void DisposeAllRunners() {
      foreach (SceneStreamsHolder holder in _streamsHolders.Values)
        holder.DisposeAttachedHolders();
      _streamsHolders.Clear();

      SceneManager.sceneUnloaded -= DisposeAttachedStreamsOnSceneUnloaded;
      SceneManager.activeSceneChanged -= ReorderStreams;
      Application.quitting -= DisposeAllRunners;
    }

    private static bool TryGetStream<TBaseSystem>(Scene scene, out ExecutionStream executionStream) {
      if (_streamsHolders.TryGetValue(scene, out SceneStreamsHolder holder)) {
        if (holder.TryGetStream<TBaseSystem>(out ExecutionStream stream)) {
          executionStream = stream;
          return true;
        }
      }

      executionStream = null;
      return false;
    }

    private static void RegisterStreamRunner<TBaseSystem>(Scene scene, StreamHolder<TBaseSystem> holder, StreamTokenSource disposeHandle) {
      if (!_streamsHolders.ContainsKey(scene))
        _streamsHolders.Add(scene, new SceneStreamsHolder());
      _streamsHolders[scene].AddStreamHolder(holder, disposeHandle);
    }

  }

}