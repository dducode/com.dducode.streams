#if UNITY_2023_1_OR_NEWER
using System;
using Streams.StreamTasks;
using Streams.StreamTasks.Internal;
using Streams.StreamTasks.TaskSources;
using UnityEngine;

namespace Streams.Extensions {

  public static class UnityAwaitableExtensions {

    public static StreamTask ToStreamTask(this Awaitable awaitable) {
      Awaitable.Awaiter awaiter = awaitable.GetAwaiter();
      if (awaiter.IsCompleted) {
        try {
          awaiter.GetResult();
          return StreamTask.CompletedTask;
        }
        catch (OperationCanceledException) {
          return StreamTask.FromCanceled();
        }
        catch (Exception e) {
          return StreamTask.FromException(e);
        }
      }

      var source = TaskSourcePool.Get<AwaitableContinuationSource>();
      source.Setup(awaitable);
      StreamTaskHelper.GetRunningStream().AddInvokableTaskSource(source);
      return source.Task;
    }

    public static StreamTask<TResult> ToStreamTask<TResult>(this Awaitable<TResult> awaitable) {
      Awaitable<TResult>.Awaiter awaiter = awaitable.GetAwaiter();
      if (awaiter.IsCompleted) {
        try {
          return StreamTask.FromResult(awaiter.GetResult());
        }
        catch (OperationCanceledException) {
          return StreamTask.FromCanceled<TResult>();
        }
        catch (Exception e) {
          return StreamTask.FromException<TResult>(e);
        }
      }

      var source = TaskSourcePool.Get<AwaitableContinuationSource<TResult>>();
      source.Setup(awaitable);
      StreamTaskHelper.GetRunningStream().AddInvokableTaskSource(source);
      return source.Task;
    }

  }

}
#endif