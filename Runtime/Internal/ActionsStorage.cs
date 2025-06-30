using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Streams.StreamActions;

namespace Streams.Internal {

  internal sealed class ActionsStorage : IEnumerable<StreamActionBase> {

    public int Count => _actions.Count;
    public StreamActionBase this[int index] => _actions[index];

    private readonly List<StreamActionBase> _actions = new();

    private readonly ConcurrentQueue<StreamActionBase> _pendingAddActions = new();
    private readonly ConcurrentQueue<StreamActionBase> _pendingRemoveActions = new();

    private readonly Action _setDirty;
    private bool _dirty;

    private readonly StreamActionComparer _comparer = new();

    public ActionsStorage() {
      _setDirty = () => _dirty = true;
    }

    public void Add(StreamActionBase action) {
      if (!_pendingAddActions.Contains(action))
        _pendingAddActions.Enqueue(action);

      if (action is IConfigurable configurable)
        configurable.OnPriorityChanged += _setDirty;
    }

    public void Remove(StreamActionBase action) {
      _pendingRemoveActions.Enqueue(action);
    }

    public void Refresh() {
      ApplyChanges();

      if (_dirty) {
        _actions.Sort(_comparer);
        _dirty = false;
      }
    }

    public void CopyFrom(ActionsStorage other) {
      foreach (StreamActionBase action in other)
        _actions.Add(action);
      _actions.Sort(_comparer);
    }

    public Enumerator GetEnumerator() {
      return new Enumerator(_actions);
    }

    IEnumerator<StreamActionBase> IEnumerable<StreamActionBase>.GetEnumerator() {
      return GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator() {
      return GetEnumerator();
    }

    public void Clear() {
      _actions.Clear();
      _pendingAddActions.Clear();
      _pendingRemoveActions.Clear();
    }

    private void ApplyChanges() {
      while (_pendingAddActions.TryDequeue(out StreamActionBase action)) {
        _actions.Add(action);
        _dirty = true;
      }

      while (_pendingRemoveActions.TryDequeue(out StreamActionBase action)) {
        _actions.Remove(action);
        _dirty = true;
      }
    }

    public struct Enumerator : IEnumerator<StreamActionBase> {

      public StreamActionBase Current => _list[_index];
      object IEnumerator.Current => Current;

      private readonly List<StreamActionBase> _list;
      private int _index;

      public Enumerator(List<StreamActionBase> list) {
        _list = list;
        _index = -1;
      }

      public bool MoveNext() {
        return ++_index < _list.Count;
      }

      public void Reset() {
      }

      public void Dispose() {
      }

    }

  }

}