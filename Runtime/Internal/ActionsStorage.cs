using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

namespace StreamsForUnity.Internal {

  internal sealed class ActionsStorage {

    public int Count => _actions.Count;

    private readonly SortedList<StreamAction, ActionLifecycle> _actions = new(new StreamActionComparer());
    private readonly Queue<StreamAction> _actionsQueueToRemove = new();

    // used for optimization
    private IEnumerator<KeyValuePair<StreamAction, ActionLifecycle>> _actionsEnumerator;

    public void Add(StreamAction action, float time, CancellationToken token) {
      _actions.Add(action, new ActionLifecycle(time));
      _actionsEnumerator = _actions.GetEnumerator();
      token.Register(() => _actionsQueueToRemove.Enqueue(action));
    }

    public void Refresh() {
      RemoveCompletedActions();
    }

    public void TickTime(float time) {
      foreach (KeyValuePair<StreamAction, ActionLifecycle> pair in this) {
        ActionLifecycle lifecycle = pair.Value;
        StreamAction action = pair.Key;

        lifecycle.remainingTime -= time;
        if (lifecycle.remainingTime <= 0 && !_actionsQueueToRemove.Contains(action))
          _actionsQueueToRemove.Enqueue(action);
      }
    }

    public void Remove(StreamAction action) {
      if (!_actionsQueueToRemove.Contains(action))
        _actionsQueueToRemove.Enqueue(action);
    }

    public void Dispose() {
      foreach (StreamAction action in _actions.Keys)
        action.Dispose();
      _actions.Clear();
    }

    public Enumerator GetEnumerator() {
      return new Enumerator(_actionsEnumerator);
    }

    private void RemoveCompletedActions() {
      var invalidEnumerator = false;

      while (_actionsQueueToRemove.TryDequeue(out StreamAction action)) {
        if (!action.Executed)
          Debug.LogWarning($"Action {action} has not been executed yet");

        _actions.Remove(action);
        invalidEnumerator = true;
        action.Dispose();
      }

      if (invalidEnumerator)
        _actionsEnumerator = _actions.GetEnumerator();
    }

    public struct Enumerator : IEnumerator<KeyValuePair<StreamAction, ActionLifecycle>> {

      public KeyValuePair<StreamAction, ActionLifecycle> Current => _other.Current;
      object IEnumerator.Current => Current;
      private readonly IEnumerator<KeyValuePair<StreamAction, ActionLifecycle>> _other;

      public Enumerator(IEnumerator<KeyValuePair<StreamAction, ActionLifecycle>> other) {
        _other = other;
      }

      public bool MoveNext() {
        return _other.MoveNext();
      }

      public void Reset() {
        _other.Reset();
      }

      public void Dispose() {
        _other.Dispose();
      }

    }

  }

}