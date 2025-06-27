using System;
using System.Collections.Generic;
using Streams.Internal;
using UnityEngine.SceneManagement;

namespace Streams.StreamContexts {

  internal sealed class SceneExecutionContext : IStreamExecutionContext {

    private readonly Scene _scene;
    private readonly Dictionary<Type, ManagedExecutionStream> _streams = new();
    private readonly StreamTokenSource _disposeHandle = new();

    public SceneExecutionContext(Scene scene, StreamToken disposeToken) {
      _scene = scene;
      disposeToken.Register(_disposeHandle.Release);
      _disposeHandle.Token.Register(Dispose);
      SceneManager.activeSceneChanged += OnActiveSceneChanged;
      SceneManager.sceneUnloaded += OnSceneUnloaded;
    }

    public ExecutionStream GetStream<TSystem>() {
      return GetStream(typeof(TSystem));
    }

    public ExecutionStream GetStream(Type systemType) {
      return _streams.TryGetValue(systemType, out ManagedExecutionStream stream) ? stream : CreateStream(systemType);
    }

    internal void OnDispose(Action onDispose) {
      _disposeHandle.Token.Register(onDispose);
    }

    private ExecutionStream CreateStream(Type systemType) {
      uint priority = SceneManager.GetActiveScene() == _scene ? 0 : uint.MaxValue;
      var name = $"{_scene.name}_{NamesUtility.CreateProfilerSampleName(systemType)}";
      var stream = new ManagedExecutionStream(UnityPlayerLoop.GetStream(systemType), name) {
        Priority = priority
      };
      _streams.Add(systemType, stream);
      _disposeHandle.Token.Register(stream.Dispose);
      ExecutionContexts.All.Add(stream, this);
      stream.OnTerminate(() => {
        _streams.Remove(systemType);
        ExecutionContexts.All.Remove(stream);
      });
      return stream;
    }

    private void OnSceneUnloaded(Scene unloadedScene) {
      if (_scene == unloadedScene)
        _disposeHandle.Release();
    }

    private void Dispose() {
      _streams.Clear();
      SceneManager.activeSceneChanged -= OnActiveSceneChanged;
      SceneManager.sceneUnloaded -= OnSceneUnloaded;
    }

    private void OnActiveSceneChanged(Scene current, Scene next) {
      if (current.buildIndex == -1) // on startup the current scene index is -1
        return;

      if (_scene == current)
        ReorderStreams(uint.MaxValue);
      else if (_scene == next)
        ReorderStreams(0);
    }

    private void ReorderStreams(uint priority) {
      foreach (ManagedExecutionStream stream in _streams.Values)
        stream.Priority = priority;
    }

  }

}