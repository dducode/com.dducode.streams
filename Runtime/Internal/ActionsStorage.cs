using System;
using System.Collections;
using System.Collections.Generic;
using StreamsForUnity.StreamActions;

namespace StreamsForUnity.Internal {

  internal sealed class ActionsStorage : IEnumerable<StreamAction> {

    public int Count => _actions.Count;
    public StreamAction this[int index] => _actions[index];

    private readonly List<StreamAction> _actions = new();

    private readonly Queue<StreamAction> _pendingAddActions = new();
    private readonly Queue<StreamAction> _pendingRemoveActions = new();
    private bool _dirty;

    private readonly StreamActionComparer _comparer = new();

    public void Add(StreamAction action) {
      if (!_pendingAddActions.Contains(action))
        _pendingAddActions.Enqueue(action);

      Action remove = () => Remove(action);
      action.OnPriorityChanged += () => _dirty = true;
      action.OnCancel += remove;
      if (action is ICompletable completable)
        completable.OnComplete += remove;
    }

    public void Remove(StreamAction action) {
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
      foreach (StreamAction action in other)
        _actions.Add(action);
      _actions.Sort(_comparer);
    }

    public Enumerator GetEnumerator() {
      return new Enumerator(_actions);
    }

    IEnumerator<StreamAction> IEnumerable<StreamAction>.GetEnumerator() {
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
      while (_pendingAddActions.TryDequeue(out StreamAction action)) {
        _actions.Add(action);
        _dirty = true;
      }

      while (_pendingRemoveActions.TryDequeue(out StreamAction action)) {
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