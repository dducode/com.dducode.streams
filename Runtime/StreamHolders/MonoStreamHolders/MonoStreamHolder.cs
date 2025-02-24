using System;
using StreamsForUnity.Internal.Extensions;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace StreamsForUnity.StreamHolders.MonoStreamHolders {

  public abstract class MonoStreamHolderBase : MonoBehaviour, IStreamHolder {

    [SerializeField] protected UpdatableBehaviour[] connectedBehaviours;

    public abstract ExecutionStream Stream { get; }
    public abstract uint Priority { get; set; }

    public IStreamHolder Join(IStreamHolder other) {
      if (other.Priority < Priority)
        return other.Join(this);

      Stream.Join(other.Stream);
      other.Dispose();
      return this;
    }

    public void Dispose() {
      DestroyImmediate(gameObject);
    }

  }

  public abstract class MonoStreamHolder<TBaseSystem> : MonoStreamHolderBase, IConfiguredStreamHolder {

    public override ExecutionStream Stream => _stream ??= CreateStream();

    public override uint Priority {
      get => _priority;
      set {
        if (_priority == value)
          return;

        ChangePriority(value);
      }
    }

    public float Delta {
      get => _delta ?? throw new ArgumentNullException(nameof(Delta));
      set {
        if (value <= 0f)
          throw new ArgumentOutOfRangeException(nameof(Delta), "Delta cannot be negative or zero");
        if (_delta.HasValue && Mathf.Approximately(_delta.Value, value))
          return;

        _execution.SetDelta((_delta = value).Value);
      }
    }

    public uint TickRate {
      get => _tickRate;
      set {
        if (value == 0)
          throw new ArgumentOutOfRangeException(nameof(TickRate), "Tick rate cannot be zero");
        if (_tickRate == value)
          return;

        _execution.SetTickRate(_tickRate = value);
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
    private float? _delta;
    private uint _tickRate = 1;

    public ExecutionStream CreateNested<THolder>(string streamName = "StreamHolder") where THolder : MonoStreamHolder<TBaseSystem> {
      return _streamHolderFactory.Create<THolder>(_transform, streamName).Stream;
    }

    public void ResetDelta() {
      _delta = null;
      _execution.ResetDelta();
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
      _stream.Lock(_lockHandle.Token);
    }

    private void OnDestroy() {
      _subscriptionHandle.Release();
      _destroyHandle.Release();
    }

    private ExecutionStream CreateStream() {
      Initialize();
      var stream = new ExecutionStream(_destroyHandle.Token, _gameObject.name);
      SetupStream(stream);
      return stream;
    }

    private void Initialize() {
      _transform = transform;
      _gameObject = gameObject;
      _parent = _transform.parent;
      _scene = _gameObject.scene;
      _priority = (uint)_transform.GetSiblingIndex();
      _destroyHandle = new StreamTokenSource();
      Streams.Get<TBaseSystem>().Add(AutoReconnect, _destroyHandle.Token);
    }

    private void SetupStream(ExecutionStream stream) {
      _subscriptionHandle = new StreamTokenSource();
      _execution = GetBaseStream(_transform.parent).Add(stream.Update, _subscriptionHandle.Token, _priority);

      foreach (UpdatableBehaviour behaviour in connectedBehaviours) {
        if (behaviour.RunOnBackgroundThread)
          stream.AddParallel(behaviour.UpdateFunction, behaviour.destroyCancellationToken);
        else
          stream.Add(behaviour.UpdateFunction, behaviour.destroyCancellationToken);
      }
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

      _execution = GetBaseStream(_transform.parent).Add(_stream.Update, _subscriptionHandle.Token, _priority).SetTickRate(_tickRate);
      if (_delta.HasValue)
        _execution.SetDelta(_delta.Value);
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