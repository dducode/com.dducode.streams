using System.Collections.Generic;
using System.Threading;
using UnityEngine;

namespace StreamsForUnity.Internal {

  internal sealed class ActionsStorage {

    public int Count => _actions.Count;

    private readonly SortedList<StreamAction, ActionLifecycle> _actions = new(new StreamActionComparer());
    private readonly Queue<StreamAction> _actionsQueueToRemove = new();

    // used for optimization
    private readonly Dictionary<StreamAction, float> _actionTimes = new();
    private IEnumerator<KeyValuePair<StreamAction, ActionLifecycle>> _actionsEnumerator;
    private readonly List<StreamAction> _persistentQueueToUpdate = new();
    private bool _queueIsDirty;

    public void Add(StreamAction action, float time, CancellationToken token) {
      _actions.Add(action, new ActionLifecycle(time));
      _actionTimes.Add(action, time);
      _actionsEnumerator = _actions.GetEnumerator();
      token.Register(() => _actionsQueueToRemove.Enqueue(action));
      _queueIsDirty = true;
    }

    public void Refresh() {
      RemoveCompletedActions();
      if (_queueIsDirty)
        RebuildPersistentQueue();
    }

    public void TickTime(float time) {
      while (_actionsEnumerator.MoveNext()) {
        StreamAction action = _actionsEnumerator.Current.Key;
        ActionLifecycle lifecycle = _actionsEnumerator.Current.Value;

        lifecycle.remainingTime -= time;
        _actionTimes[action] = lifecycle.remainingTime;
        if (lifecycle.remainingTime <= 0 && !_actionsQueueToRemove.Contains(action))
          _actionsQueueToRemove.Enqueue(action);
      }

      _actionsEnumerator.Reset();
    }

    public List<StreamAction> GetActionsToUpdate() {
      return _persistentQueueToUpdate;
    }

    public float GetRemainingTime(StreamAction action) {
      return _actionTimes[action];
    }

    public void Remove(StreamAction action) {
      if (!_actionsQueueToRemove.Contains(action))
        _actionsQueueToRemove.Enqueue(action);
    }

    public void Dispose() {
      foreach (StreamAction action in _actions.Keys)
        action.Dispose();
      _actions.Clear();
      _persistentQueueToUpdate.Clear();
    }

    private void RemoveCompletedActions() {
      var refreshEnumerator = false;

      while (_actionsQueueToRemove.TryDequeue(out StreamAction action)) {
        if (!action.Executed)
          Debug.LogWarning($"Action {action} has not been executed yet");

        _actions.Remove(action);
        _actionTimes.Remove(action);
        refreshEnumerator = true;
        _persistentQueueToUpdate.Remove(action);
        action.Dispose();
      }

      if (refreshEnumerator)
        _actionsEnumerator = _actions.GetEnumerator();
    }

    private void RebuildPersistentQueue() {
      _persistentQueueToUpdate.Clear();
      foreach (StreamAction action in _actions.Keys)
        _persistentQueueToUpdate.Add(action);

      _queueIsDirty = false;
    }

  }

}