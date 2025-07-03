using System.Threading.Tasks;
using Streams.StreamTasks;
using Streams.StreamTasks.Internal;
using Streams.StreamTasks.TaskSources;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Streams.Extensions {

  public static class TaskExtensions {

    public static StreamTask ToStreamTask(this Task task) {
      if (!TaskSourcePool.TryGet(out SystemTaskContinuationSource source))
        source = new SystemTaskContinuationSource();

      source.Setup(task);
      StreamTaskHelper.GetRunningStream().AddInvokableTaskSource(source);
      return source.Task;
    }

    public static StreamTask<TResult> ToStreamTask<TResult>(this Task<TResult> task) {
      if (!TaskSourcePool.TryGet(out SystemTaskContinuationSource<TResult> source))
        source = new SystemTaskContinuationSource<TResult>();

      source.Setup(task);
      StreamTaskHelper.GetRunningStream().AddInvokableTaskSource(source);
      return source.Task;
    }

    public static StreamTask ToStreamTask(this AsyncOperation asyncOperation) {
      if (!TaskSourcePool.TryGet(out AsyncOperationTaskSource source))
        source = new AsyncOperationTaskSource();

      source.Setup(asyncOperation);
      StreamTaskHelper.GetRunningStream().AddInvokableTaskSource(source);
      return source.Task;
    }

    public static StreamTask<Object> ToStreamTask(this ResourceRequest request) {
      if (!TaskSourcePool.TryGet(out ResourceRequestTaskSource source))
        source = new ResourceRequestTaskSource();

      source.request = request;
      StreamTaskHelper.GetRunningStream().AddInvokableTaskSource(source);
      return source.Task;
    }

#if UNITY_2023_1_OR_NEWER
    public static StreamTask ToStreamTask(this Awaitable awaitable) {
      if (!TaskSourcePool.TryGet(out AwaitableContinuationSource source))
        source = new AwaitableContinuationSource();

      source.Setup(awaitable);
      StreamTaskHelper.GetRunningStream().AddInvokableTaskSource(source);
      return source.Task;
    }

    public static StreamTask<TResult> ToStreamTask<TResult>(this Awaitable<TResult> awaitable) {
      if (!TaskSourcePool.TryGet(out AwaitableContinuationSource<TResult> source))
        source = new AwaitableContinuationSource<TResult>();

      source.Setup(awaitable);
      StreamTaskHelper.GetRunningStream().AddInvokableTaskSource(source);
      return source.Task;
    }
#endif

  }

}