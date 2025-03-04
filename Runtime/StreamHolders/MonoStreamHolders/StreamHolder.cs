using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using StreamsForUnity.Attributes;
using StreamsForUnity.Internal.Extensions;
using StreamsForUnity.StreamActions;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace StreamsForUnity.StreamHolders.MonoStreamHolders {

  /// <summary>
  /// MonoBehaviour component that contains and controls the associated <see cref="Stream"/>.
  /// It exists as a game object and controls the stream's <see cref="ManagedExecutionStream.Priority"/> using the transform sibling index.
  /// These values are closely related - when the sibling index changes, the priority changes, and vice versa
  /// </summary>
  public abstract class StreamHolder<TSystem> : MonoBehaviour, IStreamHolder, IConfigurable<StreamHolder<TSystem>>, IJoinable<StreamHolder<TSystem>> {

    [SerializeField] private bool searchInHierarchy;
    [SerializeField] private MonoBehaviour[] connectedBehaviours;
    public ExecutionStream Stream => _stream ??= CreateStream();

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
      stream.OnTerminate(() => Destroy(gameObject), _destroyHandle.Token);
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
      _scene.GetStream<TSystem>().Add(AutoReconnect, _destroyHandle.Token);
    }

    private void ConnectBehaviours(ExecutionStream stream) {
      if (connectedBehaviours == null)
        return;

      HashSet<MonoBehaviour> behaviours = connectedBehaviours.Where(behaviour => behaviour != null).ToHashSet();

      if (searchInHierarchy)
        SearchBehaviours(behaviours, _transform);

      foreach (MonoBehaviour behaviour in behaviours) {
        MethodInfo[] methods = behaviour
          .GetType()
          .GetMethods(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);

        AddPersistentActions(stream, behaviour, methods);
        AddParallelActions(stream, behaviour, methods);
      }
    }

    private ExecutionStream GetBaseStream(Transform parent) {
      if (parent != null && parent.TryGetComponentInParent(out StreamHolder<TSystem> holder))
        return holder.Stream;
      return _scene.GetStream<TSystem>();
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

    private void SearchBehaviours(HashSet<MonoBehaviour> hashSet, Transform target) {
      for (var i = 0; i < target.childCount; i++) {
        Transform child = target.GetChild(i);
        if (child == null)
          continue;

        MonoBehaviour[] behaviours = child.GetComponents<MonoBehaviour>();
        foreach (MonoBehaviour behaviour in behaviours)
          hashSet.Add(behaviour);

        if (behaviours.All(behaviour => behaviour is not IStreamHolder))
          SearchBehaviours(hashSet, child);
      }
    }

    private void AddPersistentActions(ExecutionStream stream, MonoBehaviour behaviour, MethodInfo[] methods) {
      if (behaviour is IUpdatable updatable)
        stream.Add(updatable.UpdateFunction, behaviour.destroyCancellationToken, updatable.Priority);

      IEnumerable<MethodInfo> persistentMethods = methods.Where(method => method.IsDefined(typeof(PersistentUpdateAttribute)));
      foreach (MethodInfo method in persistentMethods) {
        try {
          var action = (Action<float>)Delegate.CreateDelegate(typeof(Action<float>), behaviour, method);
          var attribute = method.GetCustomAttribute<PersistentUpdateAttribute>();
          stream.Add(action, behaviour.destroyCancellationToken, attribute.Priority);
        }
        catch (TargetParameterCountException) {
          Debug.LogError($"Method {method} has an invalid parameters count", behaviour);
        }
        catch (ArgumentException) {
          Debug.LogError($"Method {method} has an invalid signature", behaviour);
        }
      }
    }

    private void AddParallelActions(ExecutionStream stream, MonoBehaviour behaviour, MethodInfo[] methods) {
      if (behaviour is IParallelUpdatable parallelUpdatable)
        stream.AddParallel(parallelUpdatable.ParallelUpdate, behaviour.destroyCancellationToken);

      IEnumerable<MethodInfo> parallelMethods = methods.Where(method => method.IsDefined(typeof(ParallelUpdateAttribute)));
      foreach (MethodInfo method in parallelMethods) {
        try {
          var action = (Action<float>)Delegate.CreateDelegate(typeof(Action<float>), behaviour, method);
          stream.AddParallel(action, behaviour.destroyCancellationToken);
        }
        catch (TargetParameterCountException) {
          Debug.LogError($"Method {method} has an invalid parameters count", behaviour);
        }
        catch (ArgumentException) {
          Debug.LogError($"Method {method} has an invalid signature", behaviour);
        }
      }
    }

  }

}