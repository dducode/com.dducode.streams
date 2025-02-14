using System;
using System.Collections;
using System.Collections.Generic;

namespace StreamsForUnity.Internal {

  internal sealed class ActionsStorage {

    public int Count => _actions.Count;

    private readonly List<StreamAction> _actions = new(100);
    private readonly Dictionary<StreamAction, StreamToken> _tokens = new();

    private readonly Queue<StreamAction> _pendingAddActions = new(10);
    private readonly Queue<StreamAction> _pendingRemoveActions = new(10);
    private bool _dirty;

    private readonly StreamActionComparer _comparer = new();

    public void Add(StreamAction action, StreamToken token) {
      if (!_pendingAddActions.Contains(action))
        _pendingAddActions.Enqueue(action);

      Action removeAction = () => Remove(action);
      action.OnPriorityChanged += () => _dirty = true;
      action.OnComplete += removeAction;
      token.Register(removeAction);
      _tokens.Add(action, token);
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

    public void Join(ActionsStorage otherStorage) {
      otherStorage.Refresh();
      foreach (StreamAction action in otherStorage)
        Add(action, otherStorage._tokens[action]);
      otherStorage.Clear();
    }

    public Enumerator GetEnumerator() {
      return new Enumerator(_actions);
    }

    public void Dispose() {
      foreach (StreamAction action in _actions)
        action.Dispose();
      Clear();
    }

    private void Clear() {
      _actions.Clear();
      _pendingAddActions.Clear();
      _pendingRemoveActions.Clear();
      _tokens.Clear();
    }

    private void ApplyChanges() {
      while (_pendingAddActions.TryDequeue(out StreamAction action)) {
        _actions.Add(action);
        _dirty = true;
      }

      while (_pendingRemoveActions.TryDequeue(out StreamAction action)) {
        action.Dispose();
        _actions.Remove(action);
        _tokens.Remove(action);
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