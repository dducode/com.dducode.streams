using StreamsForUnity.Internal.Extensions;
using StreamsForUnity.StreamActions;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace StreamsForUnity.StreamHolders.MonoStreamHolders {

  public abstract class StreamHolderBase : MonoBehaviour, IStreamHolder {

    public abstract ExecutionStream Stream { get; }

  }

  /// <summary>
  /// MonoBehaviour component that contains and controls the associated <see cref="Stream"/>.
  /// It exists as a game object and controls the stream's <see cref="ManagedExecutionStream.Priority"/> using the transform sibling index.
  /// These values are closely related - when the sibling index changes, the priority changes, and vice versa
  /// </summary>
  public abstract class StreamHolder<TSystem> : StreamHolderBase, IConfigurable<StreamHolder<TSystem>>, IJoinable<StreamHolder<TSystem>> {

    [SerializeField] private UpdatableBehaviour[] connectedBehaviours;
    public override ExecutionStream Stream => _stream ??= CreateStream();

    private readonly MonoStreamHolderFactory _streamHolderFactory = new();
    private ManagedExecutionStream _stream;

    private StreamTokenSource _lockHandle;
    private StreamTokenSource _destroyHandle;

    private Transform _transform;
    private GameObject _gameObject;
    private Transform _parent;
    private Scene _scene;
    private int _siblingIndex;

    public ExecutionStream CreateNested<THolder>(string holderName = "StreamHolder") where THolder : StreamHolder<TSystem> {
      return _streamHolderFactory.Create<THolder>(_transform, holderName).Stream;
    }

    public StreamHolder<TSystem> SetDelta(float value) {
      _stream.Delta = value;
      return this;
    }

    public StreamHolder<TSystem> ResetDelta() {
      _stream.ResetDelta();
      return this;
    }

    public StreamHolder<TSystem> SetTickRate(uint value) {
      _stream.TickRate = value;
      return this;
    }

    public StreamHolder<TSystem> Join(StreamHolder<TSystem> other) {
      if (other._stream.Priority < _stream.Priority)
        return other.Join(this);

      _stream.Join(other._stream);
      return this;
    }

    public override string ToString() {
      return gameObject.name;
    }

    private void Start() {
      _stream ??= CreateStream();
    }

    private void OnEnable() {
      _lockHandle?.Release();
      _lockHandle = null;
    }

    private void OnDisable() {
      if (_stream.State == StreamState.Terminating)
        return;
      _lockHandle = new StreamTokenSource();
      _stream.Lock(_lockHandle.Token);
    }

    private void OnDestroy() {
      _destroyHandle.Release();
    }

    private ManagedExecutionStream CreateStream() {
      Initialize();
      var stream = new ManagedExecutionStream(GetBaseStream(_parent), _gameObject.name, (uint)_siblingIndex);
      stream.OnTerminate += () => {
        if (_destroyHandle.Released)
          return;
        Destroy(gameObject);
      };
      _destroyHandle.Register(stream.Dispose);
      ConnectBehaviours(stream);
      return stream;
    }

    private void Initialize() {
      _transform = transform;
      _gameObject = gameObject;
      _parent = _transform.parent;
      _scene = _gameObject.scene;
      _siblingIndex = _transform.GetSiblingIndex();
      _destroyHandle = new StreamTokenSource();
      Streams.Get<TSystem>().Add(AutoReconnect, _destroyHandle.Token);
    }

    private void ConnectBehaviours(ExecutionStream stream) {
      if (connectedBehaviours == null)
        return;

      foreach (UpdatableBehaviour behaviour in connectedBehaviours) {
        if (behaviour.RunOnBackgroundThread)
          stream.AddParallel(behaviour.UpdateFunction, behaviour.destroyCancellationToken);
        else
          stream.Add(behaviour.UpdateFunction, behaviour.destroyCancellationToken);
      }
    }

    private ExecutionStream GetBaseStream(Transform parent) {
      if (parent != null && parent.TryGetComponentInParent(out StreamHolder<TSystem> holder))
        return holder.Stream;
      return _gameObject.scene.GetStream<TSystem>();
    }

    private void AutoReconnect(float _) {
      if (_transform.parent != _parent || _gameObject.scene != _scene)
        ReconnectStream();

      if (_siblingIndex != _stream.Priority)
        ChangePriority((int)_stream.Priority);
      else if (_siblingIndex != _transform.GetSiblingIndex())
        ChangePriority(_transform.GetSiblingIndex());
    }

    private void ReconnectStream() {
      _parent = _transform.parent;
      _scene = _gameObject.scene;
      var priority = (int)_stream.Priority;
      _stream.Reconnect(GetBaseStream(_parent));
      ChangePriority(priority);
    }

    private void ChangePriority(int priority) {
      priority = Mathf.Clamp(priority, 0, GetMaxObjectsInHierarchy());
      _transform.SetSiblingIndex(priority);
      _stream.Priority = (uint)priority;
      _siblingIndex = priority;
    }

    private int GetMaxObjectsInHierarchy() {
      return _parent != null ? _parent.childCount - 1 : _scene.rootCount - 1;
    }

  }

}