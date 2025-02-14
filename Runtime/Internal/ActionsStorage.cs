using System;
using System.Collections;
using System.Collections.Generic;

namespace StreamsForUnity.Internal {

  internal sealed class ActionsStorage {

    public int Count => _actions.Count;

    private readonly List<StreamAction> _actions = new(100);
    private readonly Queue<StreamAction> _pendingAddActions = new(10);
    private readonly Queue<StreamAction> _pendingRemoveActions = new(10);
    private readonly StreamActionComparer _comparer = new();
    private bool _dirty;

    public void Add(StreamAction action, StreamToken token) {
      if (!_pendingAddActions.Contains(action))
        _pendingAddActions.Enqueue(action);

      Action removeAction = () => Remove(action);
      action.OnPriorityChanged += () => _dirty = true;
      action.OnComplete += removeAction;
      token.Register(removeAction);
    }

    public void Remove(StreamAction action) {
      if (!_pendingRemoveActions.Contains(action))
        _pendingRemoveActions.Enqueue(action);
    }

    public void Refresh() {
      ApplyChanges();

      if (_dirty) {
        _actions.Sort(_comparer);
        _dirty = false;
      }
    }

    public Enumerator GetEnumerator() {
      return new Enumerator(_actions);
    }

    public void Dispose() {
      foreach (StreamAction action in _actions)
        action.Dispose();
      _actions.Clear();
    }

    private void ApplyChanges() {
      while (_pendingAddActions.TryDequeue(out StreamAction action)) {
        _actions.Add(action);
        _dirty = true;
      }

      while (_pendingRemoveActions.TryDequeue(out StreamAction action)) {
        action.Dispose();
        _actions.Remove(action);
        _dirty = true;
      }
    }

    public struct Enumerator : IEnumerator<StreamAction> {

      public StreamAction Current => _list[_index];
      object IEnumerator.Current => Current;

      private readonly List<StreamAction> _list;
      private int _index;

      public Enumerator(List<StreamAction> list) {
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