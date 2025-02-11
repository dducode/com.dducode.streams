using System.Collections.Generic;
using UnityEngine;

namespace StreamsForUnity.StreamTasks.Internal {

  internal static class StreamTaskScheduler {

    private static readonly Dictionary<StreamTask, Queue<StreamTask>> _tasksSequence = new();

    internal static void Schedule(StreamTask parent, StreamTask task) {
      if (!_tasksSequence.ContainsKey(parent))
        _tasksSequence.Add(parent, new Queue<StreamTask>());
      _tasksSequence[parent].Enqueue(task);
    }

    internal static void RunNext(StreamTask parentTask) {
      if (!_tasksSequence.TryGetValue(parentTask, out Queue<StreamTask> queue)) {
        Debug.LogWarning($"No tasks attached to parent task {parentTask}");
        return;
      }

      if (queue.TryDequeue(out StreamTask task))
        task.SetResult();
      else
        _tasksSequence.Remove(parentTask);
    }

  }

}