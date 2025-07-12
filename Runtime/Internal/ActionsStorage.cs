using System;
using System.Collections;
using System.Collections.Generic;
using Streams.StreamActions;
using UnityEngine;

namespace Streams.Internal {

  internal sealed class ActionsStorage : IEnumerable<IInvokable>, IDisposable {

    public int Count => _actions.Count;
    public bool Sorted { get; set; } = true;
    public IInvokable this[int index] => _actions[index];

    private readonly List<IInvokable> _actions = new();

    private readonly Queue<IInvokable> _pendingAddActions = new();
    private readonly Queue<IInvokable> _pendingRemoveActions = new();
    private readonly object _lock = new();

    private readonly Action _setDirty;
    private bool _dirty;

    private readonly InvokableComparer _comparer = new();

    public ActionsStorage() {
      _setDirty = () => _dirty = true;
    }

    public void Add(IInvokable action) {
      lock (_lock)
        if (!_pendingAddActions.Contains(action))
          _pendingAddActions.Enqueue(action);

      if (action is IConfigurable configurable)
        configurable.OnPriorityChanged += _setDirty;
    }

    public void Remove(IInvokable action) {
      lock (_lock)
        _pendingRemoveActions.Enqueue(action);
    }

    public void ApplyAdding() {
      while (_pendingAddActions.TryDequeue(out IInvokable invokable))
        HandleAndAddObject(invokable);

      if (!Sorted)
        return;

      if (_dirty) {
        _actions.Sort(_comparer);
        _dirty = false;
      }
    }

    public void ApplyRemoving() {
      while (_pendingRemoveActions.TryDequeue(out IInvokable invokable))
        HandleAndRemoveObject(invokable);
    }

    public void CopyFrom(ActionsStorage other) {
      foreach (IInvokable invokable in other._pendingAddActions)
        _pendingAddActions.Enqueue(invokable);
      foreach (IInvokable invokable in other._actions)
        _actions.Add(invokable);
      foreach (IInvokable invokable in other._pendingRemoveActions)
        _pendingRemoveActions.Enqueue(invokable);

      _actions.Sort(_comparer);
    }

    public Enumerator GetEnumerator() {
      return new Enumerator(this);
    }

    public void Dispose() {
      for (var i = 0; i < Count; i++)
        if (this[i] is IDisposable disposable)
          DisposeInvokable(disposable);

      Clear();
    }

    public void Clear() {
      _actions.Clear();
      _pendingAddActions.Clear();
      _pendingRemoveActions.Clear();
    }

    IEnumerator<IInvokable> IEnumerable<IInvokable>.GetEnumerator() {
      return GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator() {
      return GetEnumerator();
    }

    private void HandleAndAddObject(IInvokable invokable) {
      if (invokable is IInitializable initializable) {
        try {
          initializable.Initialize();
        }
        catch (Exception e) {
          Debug.LogError($"An error occurred while initialize object with type <b>{initializable.GetType()}</b>");
          Debug.LogException(e);
          return;
        }
      }

      _actions.Add(invokable);
      _dirty = true;
    }

    private void HandleAndRemoveObject(IInvokable invokable) {
      if (invokable is IDisposable disposable)
        DisposeInvokable(disposable);

      _actions.Remove(invokable);
      _dirty = true;
    }

    private void DisposeInvokable(IDisposable disposable) {
      try {
        disposable.Dispose();
      }
      catch (Exception e) {
        Debug.LogError($"An error occurred while dispose object with type <b>{disposable.GetType()}</b>");
        Debug.LogException(e);
      }
    }

    public struct Enumerator : IEnumerator<IInvokable> {

      public IInvokable Current => _storage[_index];
      object IEnumerator.Current => Current;

      private readonly ActionsStorage _storage;
      private int _index;

      public Enumerator(ActionsStorage storage) {
        _storage = storage;
        _index = -1;
        _storage.ApplyAdding();
      }

      public bool MoveNext() {
        return ++_index < _storage.Count;
      }

      public void Reset() {
      }

      public void Dispose() {
        _storage.ApplyRemoving();
      }

    }

  }

}