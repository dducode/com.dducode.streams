using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using Streams.StreamTasks.Internal;

namespace Streams.StreamTasks {

  public partial class StreamTask {

    public static StreamTask Yield(StreamToken token = default) {
      var task = new StreamTask();
      StreamTaskHelper.GetRunningStream().AddOnce(task.SetResult, token);
      token.Register(task.SetCanceled);
      return task;
    }

    public static StreamTask Delay(int milliseconds, StreamToken token = default) {
      switch (milliseconds) {
        case < 0:
          throw new ArgumentOutOfRangeException(nameof(milliseconds));
        case 0:
          return CompletedTask;
      }

      var task = new StreamTask();
      StreamTaskHelper.GetRunningStream().AddTimer(milliseconds / 1000f, task.SetResult, token);
      token.Register(task.SetCanceled);
      return task;
    }

    public static StreamTask WaitWhile([NotNull] Func<bool> condition, StreamToken token = default) {
      if (condition == null)
        throw new ArgumentNullException(nameof(condition));
      if (!condition())
        return CompletedTask;

      var task = new StreamTask();
      var cts = new StreamTokenSource();
      StreamTaskHelper.GetRunningStream().Add(_ => {
        if (condition())
          return;

        task.SetResult();
        cts.Release();
      }, cts.Token);

      token.Register(() => {
        task.SetCanceled();
        cts.Release();
      });
      return task;
    }

    public static StreamTask WhenAll([NotNull] params StreamTask[] tasks) {
      return WhenAll((ICollection<StreamTask>)tasks);
    }

    public static StreamTask WhenAll([NotNull] ICollection<StreamTask> tasks) {
      if (tasks == null)
        throw new ArgumentNullException(nameof(tasks));
      if (tasks.Count == 0)
        return CompletedTask;

      var task = new StreamTask();
      int tasksCount = tasks.Count;
      var completedTasks = 0;
      var increment = new Action(() => {
        ++completedTasks;
        if (completedTasks == tasksCount)
          task.SetResult();
      });

      foreach (StreamTask otherTask in tasks)
        otherTask.ContinueWith(increment);
      return task;
    }

    public static StreamTask WhenAny([NotNull] params StreamTask[] tasks) {
      return WhenAny((ICollection<StreamTask>)tasks);
    }

    public static StreamTask WhenAny([NotNull] ICollection<StreamTask> tasks) {
      if (tasks == null)
        throw new ArgumentNullException(nameof(tasks));
      if (tasks.Count == 0)
        return CompletedTask;

      var task = new StreamTask();
      var continuation = new Action(() => task.SetResult());
      foreach (StreamTask otherTask in tasks)
        otherTask.ContinueWith(continuation);
      return task;
    }

  }

}