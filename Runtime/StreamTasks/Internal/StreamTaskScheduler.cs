using System.Collections.Generic;

namespace StreamsForUnity.StreamTasks.Internal {

  internal static class StreamTaskScheduler {

    private static readonly Dictionary<StreamTask, StreamTask> _tasksSequence = new();

    internal static void Schedule(StreamTask parent, StreamTask task) {
      _tasksSequence.Add(parent, task);
    }

    internal static void FireCompleted(StreamTask parentTask) {
      if (_tasksSequence.TryGetValue(parentTask, out StreamTask task)) {
        task.SetResult();
        _tasksSequence.Remove(parentTask);
      }
    }

  }

}