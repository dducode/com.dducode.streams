using System;
using System.Collections.Generic;
using Streams.Extensions;
using UnityEngine;
using UnityEngine.PlayerLoop;
using UnityEngine.SceneManagement;

namespace Streams.StreamContexts {

  [DisallowMultipleComponent]
  public class GameObjectExecutionContext : MonoBehaviour, IStreamExecutionContext {

    private readonly Dictionary<Type, ManagedExecutionStream> _streams = new();

    private StreamTokenSource _lockHandle;

    private bool _initialized;
    private Transform _transform;
    private GameObject _gameObject;
    private Transform _parent;
    private Scene _scene;
    private int _siblingIndex;

    public ExecutionStream GetStream<TSystem>() {
      return GetStream(typeof(TSystem));
    }

    public ExecutionStream GetStream(Type systemType) {
      if (!_initialized)
        Initialize();

      return _streams.TryGetValue(systemType, out ManagedExecutionStream stream) ? stream : CreateStream(systemType);
    }

    public override string ToString() {
      return _gameObject.name;
    }

    private void Awake() {
      if (!_initialized)
        Initialize();
    }

    private void Initialize() {
      _transform = transform;
      _gameObject = gameObject;
      _parent = _transform.parent;
      _scene = _gameObject.scene;
      _siblingIndex = _transform.GetSiblingIndex();
      _scene.GetStream<Update>().Add(_ => AutoReconnect(), destroyCancellationToken);
      _initialized = true;
    }

    private void OnEnable() {
      _lockHandle?.Release();
      _lockHandle = null;
    }

    private void OnDisable() {
      _lockHandle = new StreamTokenSource();
      foreach (ManagedExecutionStream stream in _streams.Values)
        stream.Lock(_lockHandle.Token);
    }

    private ExecutionStream CreateStream(Type systemType) {
      var stream = new ManagedExecutionStream(GetBaseStream(systemType), $"{_gameObject.name}_{systemType.Name}") {
        Priority = (uint)_siblingIndex
      };
      destroyCancellationToken.Register(stream.Dispose);
      _streams.Add(systemType, stream);
      ExecutionContexts.All.Add(stream, this);
      stream.OnTerminate(() => {
        _streams.Remove(systemType);
        ExecutionContexts.All.Remove(stream);
      });
      return stream;
    }

    private ExecutionStream GetBaseStream(Type systemType) {
      if (_parent != null && _parent.TryGetComponentInParent(out GameObjectExecutionContext context))
        return context.GetStream(systemType);
      return _scene.GetStream(systemType);
    }

    private void AutoReconnect() {
      if (_transform.parent != _parent || _gameObject.scene != _scene)
        ReconnectStream();

      int currentSiblingIndex = _transform.GetSiblingIndex();
      if (_siblingIndex != currentSiblingIndex)
        ChangePriority(currentSiblingIndex);
    }

    private void ReconnectStream() {
      _parent = _transform.parent;
      _scene = _gameObject.scene;
      foreach ((Type systemType, ManagedExecutionStream stream) in _streams)
        stream.Reconnect(GetBaseStream(systemType), (uint)(_siblingIndex = transform.GetSiblingIndex()));
    }

    private void ChangePriority(int siblingIndex) {
      foreach (ManagedExecutionStream stream in _streams.Values)
        stream.Priority = (uint)siblingIndex;
      _siblingIndex = siblingIndex;
    }

  }

}