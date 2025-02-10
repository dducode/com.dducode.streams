using System;
using System.Threading;
using JetBrains.Annotations;

namespace StreamsForUnity.StreamTasks {

  public partial class StreamTask {

    public static StreamTask Yield(CancellationToken token = default) {
      var task = new StreamTask();
      GetRunningStream().AddOnce(task.SetResult, token);
      token.Register(task.SetCanceled);
      return task;
    }

    public static StreamTask Delay(int milliseconds, CancellationToken token = default) {
      switch (milliseconds) {
        case < 0:
          throw new ArgumentOutOfRangeException(nameof(milliseconds));
        case 0:
          return CompletedTask;
      }

      var task = new StreamTask();
      GetRunningStream().AddTimer(milliseconds / 1000f, task.SetResult, token);
      token.Register(task.SetCanceled);
      return task;
    }

    public static StreamTask WaitWhile([NotNull] Func<bool> condition, CancellationToken token = default) {
      if (condition == null)
        throw new ArgumentNullException(nameof(condition));

      var task = new StreamTask();
      var cts = new CancellationTokenSource();
      GetRunningStream().Add(_ => {
        if (condition())
          return;

        task.SetResult();
        cts.Cancel();
      }, cts.Token);

      token.Register(() => {
        task.SetCanceled();
        cts.Cancel();
      });
      return task;
    }

    public static StreamTask WhenAll([NotNull] params StreamTask[] tasks) {
      if (tasks == null)
        throw new ArgumentNullException(nameof(tasks));
      if (tasks.Length == 0)
        return CompletedTask;

      var task = new StreamTask();
      int tasksCount = tasks.Length;
      var completedTasks = 0;
      var decrement = new Action(() => {
        ++completedTasks;
        if (completedTasks == tasksCount)
          task.SetResult();
      });

      foreach (StreamTask otherTask in tasks)
        otherTask.ContinueWith(decrement);
      return task;
    }

    public static StreamTask WhenAny([NotNull] params StreamTask[] tasks) {
      if (tasks == null)
        throw new ArgumentNullException(nameof(tasks));
      if (tasks.Length == 0)
        return CompletedTask;

      var task = new StreamTask();
      var continuation = new Action(() => task.SetResult());
      foreach (StreamTask otherTask in tasks)
        otherTask.ContinueWith(continuation);
      return task;
    }

    private static ExecutionStream GetRunningStream() {
      return Streams.RunningStream ?? throw new StreamsException("Cannot use stream tasks outside of stream execution");
    }

  }

}