using System;
using System.Collections.Generic;
using System.Threading;
using JetBrains.Annotations;
using Streams.StreamContexts;
using Streams.StreamTasks.Internal;

namespace Streams.StreamTasks {

  public partial class StreamTask {

    public static StreamTask Yield() {
      var task = new StreamTask();
      StreamTaskHelper.GetRunningStream().AddOnce(task.SetResult);
      return task;
    }

    public static StreamTask ContinueOnStream<TSystemType>() {
      var task = new StreamTask();
      Contexts.All.GetValueOrDefault(StreamTaskHelper.GetRunningStream()).GetStream<TSystemType>().AddOnce(task.SetResult);
      return task;
    }

    public static StreamTask Delay(int milliseconds) {
      return Delay(milliseconds, CancellationToken.None);
    }

    public static StreamTask Delay(int milliseconds, CancellationToken cancellationToken) {
      switch (milliseconds) {
        case < 0:
          throw new ArgumentOutOfRangeException(nameof(milliseconds));
        case 0:
          return CompletedTask;
      }

      var task = new StreamTask();
      StreamTaskHelper.GetRunningStream().AddTimer(milliseconds / 1000f, task.SetResult, cancellationToken);
      cancellationToken.Register(task.SetCanceled);
      return task;
    }

    public static StreamTask WaitWhile([NotNull] Func<bool> condition) {
      return WaitWhile(condition, CancellationToken.None);
    }

    public static StreamTask WaitWhile([NotNull] Func<bool> condition, CancellationToken cancellationToken) {
      if (condition == null)
        throw new ArgumentNullException(nameof(condition));
      if (!condition())
        return CompletedTask;

      var task = new StreamTask();
      var cts = new CancellationTokenSource();
      StreamTaskHelper.GetRunningStream().Add(_ => {
        if (condition())
          return;

        task.SetResult();
        cts.Cancel();
      }, cts.Token);

      cancellationToken.Register(() => {
        task.SetCanceled();
        cts.Cancel();
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