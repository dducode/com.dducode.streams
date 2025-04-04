using System;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

namespace Streams.StreamHolders {

  internal sealed class SceneStreamsHolder : IStreamsHolder {

    private readonly Scene _scene;
    private readonly Dictionary<Type, ManagedExecutionStream> _streams = new();
    private readonly StreamTokenSource _disposeHandle = new();

    public SceneStreamsHolder(Scene scene, StreamToken disposeToken) {
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
      var stream = new ManagedExecutionStream(UnityPlayerLoop.GetStream(systemType), _scene.name) {
        Priority = priority
      };
      _streams.Add(systemType, stream);
      _disposeHandle.Token.Register(stream.Dispose);
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
      else
        throw new InvalidOperationException($"Scene {_scene} is not current and is not next scene");
    }

    private void ReorderStreams(uint priority) {
      foreach (ManagedExecutionStream stream in _streams.Values)
        stream.Priority = priority;
    }

  }

}