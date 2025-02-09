using System.Collections.Generic;
using System.Threading;
using StreamsForUnity.Internal;
using StreamsForUnity.StreamRunners;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace StreamsForUnity {

  public static class SceneStreams {

    private static readonly Dictionary<Scene, SceneStreamsHolder> _streamHolders = new();

    public static ExecutionStream GetStream<TBaseSystem>(this Scene scene) {
      if (!scene.IsValid())
        throw new StreamsException($"Cannot get stream from invalid scene ({scene})");
      if (TryGetStream<TBaseSystem>(scene, out ExecutionStream stream))
        return stream;

      var disposeHandle = new CancellationTokenSource();
      var newStream = new ExecutionStream(disposeHandle.Token, scene.name);
      uint priority = SceneManager.GetActiveScene() == scene ? 0 : uint.MaxValue;

      RegisterStream<TBaseSystem>(scene, Streams.Get<TBaseSystem>(), newStream, disposeHandle, priority);
      return newStream;
    }

    public static ExecutionStream CreateNested<TRunner>(this Scene scene, string streamName = "StreamRunner")
      where TRunner : MonoBehaviour, IStreamRunner {
      return new MonoStreamRunnerFactory().Create<TRunner>(scene, streamName).Stream;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Initialize() {
      SceneManager.sceneUnloaded += DisposeAttachedStreamsOnSceneUnloaded;
      SceneManager.activeSceneChanged += ReorderStreams;
      Application.quitting += DisposeAllStreams;
    }

    private static void ReorderStreams(Scene current, Scene next) {
      if (current.buildIndex == -1) // on startup the current scene index is -1
        return;

      if (_streamHolders.TryGetValue(current, out SceneStreamsHolder firstHolder))
        firstHolder.ReorderStreams(uint.MaxValue);
      if (_streamHolders.TryGetValue(next, out SceneStreamsHolder secondHolder))
        secondHolder.ReorderStreams(0);
    }

    private static void DisposeAttachedStreamsOnSceneUnloaded(Scene scene) {
      if (!_streamHolders.TryGetValue(scene, out SceneStreamsHolder holder))
        return;

      holder.DisposeAttachedStreams();
      _streamHolders.Remove(scene);
    }

    private static void DisposeAllStreams() {
      foreach (SceneStreamsHolder holder in _streamHolders.Values)
        holder.DisposeAttachedStreams();
      _streamHolders.Clear();
      SceneManager.sceneUnloaded -= DisposeAttachedStreamsOnSceneUnloaded;
      SceneManager.activeSceneChanged -= ReorderStreams;
      Application.quitting -= DisposeAllStreams;
    }

    private static bool TryGetStream<TBaseSystem>(Scene scene, out ExecutionStream executionStream) {
      if (_streamHolders.TryGetValue(scene, out SceneStreamsHolder holder)) {
        if (holder.TryGetStream<TBaseSystem>(out ExecutionStream stream)) {
          executionStream = stream;
          return true;
        }
      }

      executionStream = null;
      return false;
    }

    private static void RegisterStream<TBaseSystem>(
      Scene scene, ExecutionStream baseStream, ExecutionStream newStream, CancellationTokenSource disposeHandle, uint priority
    ) {
      if (!_streamHolders.ContainsKey(scene))
        _streamHolders.Add(scene, new SceneStreamsHolder());
      _streamHolders[scene].AddStream<TBaseSystem>(baseStream, newStream, disposeHandle, priority);
    }

  }

}