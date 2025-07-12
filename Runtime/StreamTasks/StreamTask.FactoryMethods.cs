using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using Streams.StreamContexts;
using Streams.StreamTasks.Internal;
using Streams.StreamTasks.TaskSources;

namespace Streams.StreamTasks {

  public partial struct StreamTask {

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

      var source = Pool.Get<DelayedTaskSource>();
      source.Setup(milliseconds / 1000f);
      source.SetCancellation(cancellationToken);
      StreamTaskHelper.GetRunningStream().AddInvokableTaskSource(source);
      return source.Task;
    }

    public static StreamTask DelayFrame(int frames) {
      return DelayFrame(frames, StreamToken.None);
    }

    public static StreamTask DelayFrame(int frames, StreamToken cancellationToken) {
      if (frames < 0)
        throw new ArgumentOutOfRangeException(nameof(frames));
      if (cancellationToken.Released)
        return FromCanceled();
      if (frames == 0)
        return CompletedTask;

      var source = Pool.Get<DelayFrameTaskSource>();
      source.Setup(frames);
      source.SetCancellation(cancellationToken);
      StreamTaskHelper.GetRunningStream().AddInvokableTaskSource(source);
      return source.Task;
    }

    public static StreamTask WaitWhile([NotNull] Func<bool> condition) {
      if (condition == null)
        throw new ArgumentNullException(nameof(condition));
      return WaitWhile(condition, StreamToken.None);
    }

    public static StreamTask WaitWhile([NotNull] Func<bool> condition, StreamToken cancellationToken) {
      if (condition == null)
        throw new ArgumentNullException(nameof(condition));
      if (cancellationToken.Released)
        return FromCanceled();
      if (!condition())
        return CompletedTask;

      var source = Pool.Get<ConditionalTaskSource>();
      source.Setup(condition);
      source.SetCancellation(cancellationToken);
      StreamTaskHelper.GetRunningStream().AddInvokableTaskSource(source);
      return source.Task;
    }

    public static StreamTask WhenAll([NotNull] params StreamTask[] tasks) {
      if (tasks == null)
        throw new ArgumentNullException(nameof(tasks));
      if (tasks.Length == 0)
        return CompletedTask;

      for (var i = 0; i < tasks.Length; i++) {
        Awaiter awaiter = tasks[i].GetAwaiter();
        if (!awaiter.IsCompleted) {
          var source = Pool.Get<WhenAllTaskSource>();
          source.Setup(tasks);
          StreamTaskHelper.GetRunningStream().AddInvokableTaskSource(source);
          return source.Task;
        }
      }

      var exceptions = new List<Exception>(tasks.Length);

      for (var i = 0; i < tasks.Length; i++) {
        try {
          tasks[i].GetAwaiter().GetResult();
        }
        catch (Exception e) {
          exceptions.Add(e);
        }
      }

      return exceptions.Count == 0 ? CompletedTask : FromException(new AggregateException(exceptions));
    }

    public static StreamTask WhenAny([NotNull] params StreamTask[] tasks) {
      if (tasks == null)
        throw new ArgumentNullException(nameof(tasks));
      if (tasks.Length == 0)
        return CompletedTask;

      for (var i = 0; i < tasks.Length; i++) {
        Awaiter awaiter = tasks[i].GetAwaiter();
        if (!awaiter.IsCompleted)
          continue;

        try {
          SetupAwaitersCompletion();
          awaiter.GetResult();
          return CompletedTask;
        }
        catch (OperationCanceledException) {
          return FromCanceled();
        }
        catch (Exception e) {
          return FromException(e);
        }
      }

      var source = Pool.Get<WhenAnyTaskSource>();
      source.Setup(tasks);
      StreamTaskHelper.GetRunningStream().AddInvokableTaskSource(source);
      return source.Task;

      void SetupAwaitersCompletion() {
        for (var i = 0; i < tasks.Length; i++) {
          Awaiter awaiter = tasks[i].GetAwaiter();
          if (!awaiter.IsCompleted)
            awaiter.OnCompleted(awaiter.GetResult);
        }
      }
    }

    public static StreamTask<TResult> FromResult<TResult>(TResult result) {
      var source = Pool.Get<StreamTaskSource<TResult>>();
      source.SetResult(result);
      return source.Task;
    }

    public static StreamTask FromCanceled() {
      var source = Pool.Get<StreamTaskSource>();
      source.SetCanceled();
      return source.Task;
    }

    public static StreamTask<TResult> FromCanceled<TResult>() {
      var source = Pool.Get<StreamTaskSource<TResult>>();
      source.SetCanceled();
      return source.Task;
    }

    public static StreamTask FromException(Exception error) {
      var source = Pool.Get<StreamTaskSource>();
      source.SetException(error);
      return source.Task;
    }

    public static StreamTask<TResult> FromException<TResult>(Exception error) {
      var source = Pool.Get<StreamTaskSource<TResult>>();
      source.SetException(error);
      return source.Task;
    }

  }

}