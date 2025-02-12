using System.Collections.Generic;
using System.Threading;
using StreamsForUnity.Internal;
using StreamsForUnity.StreamRunners;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace StreamsForUnity {

  public static class SceneStreams {

    private static readonly Dictionary<Scene, SceneStreamRunnersHolder> _runnersHolders = new();
    private static readonly MonoStreamRunnerFactory _streamRunnerFactory = new();

    public static ExecutionStream GetStream<TBaseSystem>(this Scene scene) {
      if (!scene.IsValid())
        throw new StreamsException($"Cannot get stream from invalid scene ({scene})");
      if (TryGetStream<TBaseSystem>(scene, out ExecutionStream stream))
        return stream;

      var disposeHandle = new CancellationTokenSource();
      uint priority = SceneManager.GetActiveScene() == scene ? 0 : uint.MaxValue;
      var runner = new StreamRunner<TBaseSystem>(disposeHandle.Token, scene.name, priority);

      RegisterStreamRunner(scene, runner, disposeHandle);
      return runner.Stream;
    }

    public static ExecutionStream CreateNested<TRunner>(this Scene scene, string streamName = "StreamRunner")
      where TRunner : MonoBehaviour, IStreamRunner {
      return _streamRunnerFactory.Create<TRunner>(scene, streamName).Stream;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Initialize() {
      SceneManager.sceneUnloaded += DisposeAttachedStreamsOnSceneUnloaded;
      SceneManager.activeSceneChanged += ReorderStreams;
      Application.quitting += DisposeAllRunners;
    }

    private static void DisposeAttachedStreamsOnSceneUnloaded(Scene scene) {
      if (!_runnersHolders.TryGetValue(scene, out SceneStreamRunnersHolder holder))
        return;

      holder.DisposeAttachedRunners();
      _runnersHolders.Remove(scene);
    }

    private static void ReorderStreams(Scene current, Scene next) {
      if (current.buildIndex == -1) // on startup the current scene index is -1
        return;

      if (_runnersHolders.TryGetValue(current, out SceneStreamRunnersHolder firstHolder))
        firstHolder.ReorderRunners(uint.MaxValue);
      if (_runnersHolders.TryGetValue(next, out SceneStreamRunnersHolder secondHolder))
        secondHolder.ReorderRunners(0);
    }

    private static void DisposeAllRunners() {
      foreach (SceneStreamRunnersHolder holder in _runnersHolders.Values)
        holder.DisposeAttachedRunners();
      _runnersHolders.Clear();

      SceneManager.sceneUnloaded -= DisposeAttachedStreamsOnSceneUnloaded;
      SceneManager.activeSceneChanged -= ReorderStreams;
      Application.quitting -= DisposeAllRunners;
    }

    private static bool TryGetStream<TBaseSystem>(Scene scene, out ExecutionStream executionStream) {
      if (_runnersHolders.TryGetValue(scene, out SceneStreamRunnersHolder holder)) {
        if (holder.TryGetStream<TBaseSystem>(out ExecutionStream stream)) {
          executionStream = stream;
          return true;
        }
      }

      executionStream = null;
      return false;
    }

    private static void RegisterStreamRunner<TBaseSystem>(Scene scene, StreamRunner<TBaseSystem> runner, CancellationTokenSource disposeHandle) {
      if (!_runnersHolders.ContainsKey(scene))
        _runnersHolders.Add(scene, new SceneStreamRunnersHolder());
      _runnersHolders[scene].AddStreamRunner(runner, disposeHandle);
    }

  }

}