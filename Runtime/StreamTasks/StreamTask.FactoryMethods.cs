using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using Streams.StreamContexts;
using Streams.StreamTasks.Internal;

namespace Streams.StreamTasks {

  public partial class StreamTask {

    public static StreamYieldAwaitable Yield() {
      return new StreamYieldAwaitable(StreamTaskHelper.GetRunningStream());
    }

    public static StreamYieldAwaitable ContinueOnStream<TSystemType>() {
      return new StreamYieldAwaitable(ExecutionContexts.All.GetValueOrDefault(StreamTaskHelper.GetRunningStream()).GetStream<TSystemType>());
    }

    public static StreamTask Delay(int milliseconds) {
      return Delay(milliseconds, StreamToken.None);
    }

    public static StreamTask Delay(int milliseconds, StreamToken cancellationToken) {
      if (milliseconds < 0)
        throw new ArgumentOutOfRangeException(nameof(milliseconds));
      if (cancellationToken.Released)
        return FromCanceled();
      if (milliseconds == 0)
        return CompletedTask;

      var task = new StreamTask();
      StreamTaskHelper.GetRunningStream().AddDelayed(milliseconds / 1000f, task.SetResult);
      cancellationToken.Register(task);
      return task;
    }

    public static StreamTask<TResult> FromResult<TResult>(TResult result) {
      var task = new StreamTask<TResult>();
      task.SetResult(result);
      return task;
    }

    public static StreamTask FromCanceled() {
      var task = new StreamTask();
      task.SetCanceled();
      return task;
    }

    public static StreamTask WaitWhile([NotNull] Func<bool> condition) {
      return WaitWhile(condition, StreamToken.None);
    }

    public static StreamTask WaitWhile([NotNull] Func<bool> condition, StreamToken cancellationToken) {
      if (condition == null)
        throw new ArgumentNullException(nameof(condition));
      if (cancellationToken.Released)
        return FromCanceled();
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

      cancellationToken.Register(() => {
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
        if (++completedTasks == tasksCount)
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