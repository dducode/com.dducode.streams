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

    private ExecutionStream _stream;
    private CancellationTokenSource _lockHandle;
    private CancellationTokenSource _subscriptionHandle;

    private Scene _scene;
    private uint _siblingIndex;
    private Transform _transform;
    private GameObject _gameObject;

    public ExecutionStream CreateNested<TRunner>(string streamName = "StreamRunner") where TRunner : MonoBehaviour, IStreamRunner {
      return new MonoStreamRunnerFactory().Create<TRunner>(_transform, streamName).Stream;
    }

    private void Awake() {
      _transform = transform;
      _gameObject = gameObject;
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

    private void OnTransformParentChanged() {
      ReconnectStream();
    }

    private ExecutionStream CreateStream() {
      var stream = new ExecutionStream(destroyCancellationToken, _gameObject.name);

      _subscriptionHandle = new CancellationTokenSource();
      _scene = _gameObject.scene;
      _siblingIndex = (uint)_transform.GetSiblingIndex();

      GetBaseStream(_transform.parent).Add(stream.Update, _subscriptionHandle.Token, _siblingIndex);
      Streams.Get<TBaseSystem>().Add(ReconnectStreamIfNeeded, destroyCancellationToken);
      if (predefinedActions != null && predefinedActions.GetPersistentEventCount() > 0)
        stream.Add(predefinedActions.Invoke, destroyCancellationToken);

      _lockHandle = new CancellationTokenSource();
      stream.Lock(_lockHandle.Token);
      return stream;
    }

    private ExecutionStream GetBaseStream(Transform parent) {
      if (parent != null && parent.TryGetComponentInParent(out MonoStreamRunner<TBaseSystem> runner))
        return runner.Stream;
      return _gameObject.scene.GetStream<TBaseSystem>();
    }

    private void ReconnectStreamIfNeeded(float _) {
      if (_transform.GetSiblingIndex() == _siblingIndex && _gameObject.scene == _scene)
        return;

      ReconnectStream();
    }

    private void ReconnectStream() {
      _subscriptionHandle.Cancel();
      _subscriptionHandle = new CancellationTokenSource();
      _siblingIndex = (uint)_transform.GetSiblingIndex();
      _scene = _gameObject.scene;
      GetBaseStream(_transform.parent).Add(Stream.Update, _subscriptionHandle.Token, _siblingIndex);
    }

  }

}