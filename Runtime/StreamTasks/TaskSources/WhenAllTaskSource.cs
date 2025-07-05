using System;
using System.Collections.Generic;

namespace Streams.StreamTasks.TaskSources {

  internal sealed class WhenAllTaskSource : RunnableTaskSource<StreamTask[]> {

    private readonly List<StreamTask> _tasks = new();
    private readonly Queue<StreamTask> _removedTasks = new();
    private readonly List<Exception> _exceptions = new();

    public override void Setup(StreamTask[] value) {
      _tasks.AddRange(value);
    }

    public override bool Invoke(float deltaTime) {
      foreach (StreamTask task in _tasks) {
        StreamTaskAwaiter awaiter = task.GetAwaiter();
        if (!awaiter.IsCompleted)
          continue;

        try {
          awaiter.GetResult();
        }
        catch (Exception e) {
          _exceptions.Add(e);
        }
        finally {
          _removedTasks.Enqueue(task);
        }
      }

      while (_removedTasks.TryDequeue(out StreamTask task))
        _tasks.Remove(task);

      if (_tasks.Count > 0)
        return true;

      if (_exceptions.Count == 0)
        SetResult();
      else
        SetException(new AggregateException(_exceptions));

      return false;
    }

    private protected override void Reset() {
      base.Reset();
      _exceptions.Clear();
    }

  }

}