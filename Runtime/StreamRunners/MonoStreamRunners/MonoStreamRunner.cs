using StreamsForUnity.Internal.Extensions;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

namespace StreamsForUnity.StreamRunners.MonoStreamRunners {

  [DisallowMultipleComponent]
  public abstract class MonoStreamRunner<TBaseSystem> : MonoBehaviour, IStreamRunner {

    [SerializeField] private UnityEvent<float> predefinedActions;
    public ExecutionStream Stream => _stream ??= CreateStream();
    public uint Priority { get; private set; }

    private readonly MonoStreamRunnerFactory _streamRunnerFactory = new();
    private ExecutionStream _stream;
    private StreamAction _execution;

    private StreamTokenSource _lockHandle;
    private StreamTokenSource _subscriptionHandle;

    private Transform _transform;
    private GameObject _gameObject;
    private Transform _parent;
    private Scene _scene;

    public ExecutionStream CreateNested<TRunner>(string streamName = "StreamRunner") where TRunner : MonoBehaviour, IStreamRunner {
      return _streamRunnerFactory.Create<TRunner>(_transform, streamName).Stream;
    }

    public void ChangePriority(uint priority) {
      if (Priority != priority) {
        priority = (uint)Mathf.Clamp(priority, 0, _transform.parent.childCount - 1);
        _transform.SetSiblingIndex((int)priority);
        Priority = priority;
      }

      _execution.ChangePriority(priority);
    }

    public IStreamRunner Join(IStreamRunner other) {
      if (other.Priority < Priority)
        return other.Join(this);

      Stream.Join(other.Stream);
      other.Dispose();
      return this;
    }

    public void Dispose() {
      DestroyImmediate(_gameObject);
    }

    private void Awake() {
      _stream ??= CreateStream();
    }

    private void OnEnable() {
      _lockHandle.Release();
      _lockHandle = null;
    }

    private void OnDisable() {
      _lockHandle = new StreamTokenSource();
      Stream.Lock(_lockHandle.Token);
    }

    private void OnDestroy() {
      _subscriptionHandle.Release();
    }

    private ExecutionStream CreateStream() {
      Initialize();

      var stream = new ExecutionStream(destroyCancellationToken, _gameObject.name);
      SetupStream(stream);

      ExecutionStream rootStream = Streams.Get<TBaseSystem>();
      rootStream.Add(AutoReconnect, destroyCancellationToken);
      rootStream.Add(AutoChangePriority, destroyCancellationToken);

      _lockHandle = new StreamTokenSource();
      stream.Lock(_lockHandle.Token);
      return stream;
    }

    private void Initialize() {
      _transform = transform;
      _gameObject = gameObject;
      _parent = _transform.parent;
      _scene = _gameObject.scene;
      Priority = (uint)_transform.GetSiblingIndex();
    }

    private void SetupStream(ExecutionStream stream) {
      _subscriptionHandle = new StreamTokenSource();
      _execution = GetBaseStream(_transform.parent).Add(stream.Update, _subscriptionHandle.Token, Priority);
      if (predefinedActions != null && predefinedActions.GetPersistentEventCount() > 0)
        stream.Add(predefinedActions.Invoke, destroyCancellationToken);
    }

    private ExecutionStream GetBaseStream(Transform parent) {
      if (parent != null && parent.TryGetComponentInParent(out MonoStreamRunner<TBaseSystem> runner))
        return runner.Stream;
      return _gameObject.scene.GetStream<TBaseSystem>();
    }

    private void AutoReconnect(float _) {
      if (_transform.parent == _parent && _gameObject.scene == _scene)
        return;

      ReconnectStream();
    }

    private void AutoChangePriority(float _) {
      if (Priority == _transform.GetSiblingIndex())
        return;

      ChangePriority(Priority = (uint)_transform.GetSiblingIndex());
    }

    private void ReconnectStream() {
      _subscriptionHandle.Release();
      _subscriptionHandle = new StreamTokenSource();
      _parent = _transform.parent;
      _scene = _gameObject.scene;
      Priority = (uint)transform.GetSiblingIndex();
      _execution = GetBaseStream(_transform.parent).Add(Stream.Update, _subscriptionHandle.Token, Priority);
    }

  }

}