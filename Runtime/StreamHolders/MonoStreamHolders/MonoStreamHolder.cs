using StreamsForUnity.Internal.Extensions;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

namespace StreamsForUnity.StreamHolders.MonoStreamHolders {

  [DisallowMultipleComponent]
  public abstract class MonoStreamHolder<TBaseSystem> : MonoBehaviour, IStreamHolder {

    [SerializeField] private UnityEvent<float> predefinedActions;

    public ExecutionStream Stream => _stream ??= CreateStream();

    public uint Priority {
      get => _priority;
      set {
        if (_priority == value)
          return;

        if (_execution != null)
          ChangePriority(value);
      }
    }

    private readonly MonoStreamHolderFactory _streamHolderFactory = new();
    private ExecutionStream _stream;
    private uint _priority;

    private StreamAction _execution;
    private StreamTokenSource _lockHandle;
    private StreamTokenSource _subscriptionHandle;
    private StreamTokenSource _destroyHandle;

    private Transform _transform;
    private GameObject _gameObject;
    private Transform _parent;
    private Scene _scene;

    public ExecutionStream CreateNested<THolder>(string streamName = "StreamHolder") where THolder : MonoStreamHolder<TBaseSystem> {
      return _streamHolderFactory.Create<THolder>(_transform, streamName).Stream;
    }

    public IStreamHolder Join(IStreamHolder other) {
      if (other.Priority < Priority)
        return other.Join(this);

      Stream.Join(other.Stream);
      other.Dispose();
      return this;
    }

    public void Dispose() {
      DestroyImmediate(_gameObject);
    }

    private void Start() {
      _stream ??= CreateStream();
    }

    private void OnEnable() {
      _lockHandle?.Release();
      _lockHandle = null;
    }

    private void OnDisable() {
      _lockHandle = new StreamTokenSource();
      Stream.Lock(_lockHandle.Token);
    }

    private void OnDestroy() {
      _subscriptionHandle.Release();
      _destroyHandle.Release();
    }

    private ExecutionStream CreateStream() {
      Initialize();

      _destroyHandle = new StreamTokenSource();
      var stream = new ExecutionStream(_destroyHandle.Token, _gameObject.name);
      SetupStream(stream);

      Streams.Get<TBaseSystem>().Add(AutoReconnect, _destroyHandle.Token);
      return stream;
    }

    private void Initialize() {
      _transform = transform;
      _gameObject = gameObject;
      _parent = _transform.parent;
      _scene = _gameObject.scene;
      _priority = (uint)_transform.GetSiblingIndex();
    }

    private void SetupStream(ExecutionStream stream) {
      _subscriptionHandle = new StreamTokenSource();
      _execution = GetBaseStream(_transform.parent).Add(stream.Update, _subscriptionHandle.Token, _priority);

      if (predefinedActions != null && predefinedActions.GetPersistentEventCount() > 0)
        stream.Add(predefinedActions.Invoke, _destroyHandle.Token);
    }

    private ExecutionStream GetBaseStream(Transform parent) {
      if (parent != null && parent.TryGetComponentInParent(out MonoStreamHolder<TBaseSystem> runner))
        return runner.Stream;
      return _gameObject.scene.GetStream<TBaseSystem>();
    }

    private void AutoReconnect(float _) {
      if (_transform.parent != _parent || _gameObject.scene != _scene)
        ReconnectStream();
      if (_priority != _transform.GetSiblingIndex())
        ChangePriority(_priority = (uint)_transform.GetSiblingIndex());
    }

    private void ReconnectStream() {
      _subscriptionHandle.Release();
      _subscriptionHandle = new StreamTokenSource();
      _parent = _transform.parent;
      _scene = _gameObject.scene;
      _priority = (uint)transform.GetSiblingIndex();
      _execution = GetBaseStream(_transform.parent).Add(_stream.Update, _subscriptionHandle.Token, _priority);
    }

    private void ChangePriority(uint priority) {
      if (_priority != priority) {
        priority = (uint)Mathf.Clamp(priority, 0, _transform.parent.childCount - 1);
        _transform.SetSiblingIndex((int)priority);
        _priority = priority;
      }

      _execution.ChangePriority(priority);
    }

  }

}