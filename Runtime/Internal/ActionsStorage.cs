using System;
using System.Collections;
using System.Collections.Generic;
using Streams.StreamActions;

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

    public void Refresh() {
      ApplyChanges();

      if (_dirty) {
        if (Sorted)
          _actions.Sort(_comparer);
        _dirty = false;
      }
    }

    public void CopyFrom(ActionsStorage other) {
      foreach (IInvokable action in other)
        _actions.Add(action);
      _actions.Sort(_comparer);
    }

    public Enumerator GetEnumerator() {
      return new Enumerator(_actions);
    }

    IEnumerator<IInvokable> IEnumerable<IInvokable>.GetEnumerator() {
      return GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator() {
      return GetEnumerator();
    }

    private void ApplyChanges() {
      while (_pendingAddActions.TryDequeue(out IInvokable invokable)) {
        _actions.Add(invokable);
        _dirty = true;
      }

      while (_pendingRemoveActions.TryDequeue(out IInvokable invokable)) {
        _actions.Remove(invokable);
        _dirty = true;
      }
    }

    public void Dispose() {
      _actions.Clear();
      _pendingAddActions.Clear();
      _pendingRemoveActions.Clear();
    }

    public struct Enumerator : IEnumerator<IInvokable> {

      public IInvokable Current => _list[_index];
      object IEnumerator.Current => Current;

      private readonly List<IInvokable> _list;
      private int _index;

      public Enumerator(List<IInvokable> list) {
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