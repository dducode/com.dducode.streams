using System.Collections.Generic;
using UnityEngine;

namespace Streams.StreamTasks.Internal {

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

      if (queue.TryDequeue(out StreamTask task)) {
        if (parentTask.Error == null)
          task.SetResult();
        else
          task.SetException(parentTask.Error);
      }
      else {
        _tasksSequence.Remove(parentTask);
      }
    }

  }

}