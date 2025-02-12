using System.Threading;
using StreamsForUnity.Internal.Extensions;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

namespace StreamsForUnity.StreamRunners.MonoStreamRunners {

  [DisallowMultipleComponent]
  public abstract class MonoStreamRunner<TBaseSystem> : MonoBehaviour, IStreamRunner {

    [SerializeField] private UnityEvent<float> predefinedActions;
    public ExecutionStream Stream => _stream ??= CreateStream();

    private readonly MonoStreamRunnerFactory _streamRunnerFactory = new();
    private ExecutionStream _stream;
    private CancellationTokenSource _lockHandle;
    private CancellationTokenSource _subscriptionHandle;

    private Transform _transform;
    private GameObject _gameObject;
    private Transform _parent;
    private Scene _scene;
    private uint _siblingIndex;

    public ExecutionStream CreateNested<TRunner>(string streamName = "StreamRunner") where TRunner : MonoBehaviour, IStreamRunner {
      return _streamRunnerFactory.Create<TRunner>(_transform, streamName).Stream;
    }

    public void ReconnectStream(uint priority) {
      if (_siblingIndex != priority) {
        priority = (uint)Mathf.Clamp(priority, 0, _transform.parent.childCount - 1);
        _transform.SetSiblingIndex((int)priority);
        _siblingIndex = priority;
      }

      _subscriptionHandle.Cancel();
      _subscriptionHandle = new CancellationTokenSource();
      _parent = _transform.parent;
      _scene = _gameObject.scene;
      GetBaseStream(_transform.parent).Add(Stream.Update, _subscriptionHandle.Token, priority);
    }

    private void Awake() {
      _stream ??= CreateStream();
    }

    private void OnEnable() {
      _lockHandle.Cancel();
      _lockHandle = null;
    }

    private void OnDisable() {
      _lockHandle = new CancellationTokenSource();
      Stream.Lock(_lockHandle.Token);
    }

    private void OnDestroy() {
      _subscriptionHandle.Cancel();
    }

    private ExecutionStream CreateStream() {
      _transform = transform;
      _gameObject = gameObject;
      _parent = _transform.parent;
      _scene = _gameObject.scene;
      _siblingIndex = (uint)_transform.GetSiblingIndex();

      var stream = new ExecutionStream(destroyCancellationToken, _gameObject.name);
      _subscriptionHandle = new CancellationTokenSource();
      GetBaseStream(_transform.parent).Add(stream.Update, _subscriptionHandle.Token, _siblingIndex);
      if (predefinedActions != null && predefinedActions.GetPersistentEventCount() > 0)
        stream.Add(predefinedActions.Invoke, destroyCancellationToken);

      Streams.Get<TBaseSystem>().Add(AutoReconnect, destroyCancellationToken);

      _lockHandle = new CancellationTokenSource();
      stream.Lock(_lockHandle.Token);
      return stream;
    }

    private ExecutionStream GetBaseStream(Transform parent) {
      if (parent != null && parent.TryGetComponentInParent(out MonoStreamRunner<TBaseSystem> runner))
        return runner.Stream;
      return _gameObject.scene.GetStream<TBaseSystem>();
    }

    private void AutoReconnect(float _) {
      if (_transform.GetSiblingIndex() == _siblingIndex && _transform.parent == _parent && _gameObject.scene == _scene)
        return;

      ReconnectStream(_siblingIndex = (uint)_transform.GetSiblingIndex());
    }

  }

}