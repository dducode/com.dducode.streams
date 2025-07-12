using System;
using System.Threading.Tasks;
using Streams.StreamTasks;
using Streams.StreamTasks.Internal;
using Streams.StreamTasks.TaskSources;

namespace Streams.Extensions {

  public static class TaskExtensions {

    public static StreamTask ToStreamTask(this Task task) {
      if (task.IsCompleted) {
        try {
          task.GetAwaiter().GetResult();
          return StreamTask.CompletedTask;
        }
        catch (OperationCanceledException) {
          return StreamTask.FromCanceled();
        }
        catch (Exception e) {
          return StreamTask.FromException(e);
        }
      }

      var source = Pool.Get<SystemTaskContinuationSource>();
      source.Setup(task);
      StreamTaskHelper.GetRunningStream().AddInvokableTaskSource(source);
      return source.Task;
    }

    public static StreamTask<TResult> ToStreamTask<TResult>(this Task<TResult> task) {
      if (task.IsCompleted) {
        try {
          return StreamTask.FromResult(task.GetAwaiter().GetResult());
        }
        catch (OperationCanceledException) {
          return StreamTask.FromCanceled<TResult>();
        }
        catch (Exception e) {
          return StreamTask.FromException<TResult>(e);
        }
      }

      var source = Pool.Get<SystemTaskContinuationSource<TResult>>();
      source.Setup(task);
      StreamTaskHelper.GetRunningStream().AddInvokableTaskSource(source);
      return source.Task;
    }

  }

}