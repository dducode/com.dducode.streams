#if STREAMS_ADDRESSABLES_INTEGRATION
using System;
using Streams.StreamTasks;
using Streams.StreamTasks.Internal;
using Streams.StreamTasks.TaskSources;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace Streams.Addressables {

  public static class AddressablesExtensions {

    public static StreamTaskAwaiter<TResult> GetAwaiter<TResult>(this AsyncOperationHandle<TResult> handle) {
      return handle.ToStreamTask(StreamToken.None).GetAwaiter();
    }

    public static StreamTask<TResult> ToStreamTask<TResult>(this AsyncOperationHandle<TResult> handle, StreamToken cancellationToken) {
      if (!handle.IsValid())
        throw new InvalidOperationException("Cannot await invalid handle");

      if (handle.IsDone) {
        if (handle.OperationException != null) {
          return handle.OperationException is OperationCanceledException
            ? StreamTask.FromCanceled<TResult>()
            : StreamTask.FromException<TResult>(handle.OperationException);
        }

        return StreamTask.FromResult(handle.Result);
      }

      var source = TaskSourcePool.Get<AsyncOperationHandleTaskSource<TResult>>();
      source.Setup(handle);
      source.SetCancellation(cancellationToken);
      StreamTaskHelper.GetRunningStream().AddInvokableTaskSource(source);
      return source.Task;
    }

  }

}
#endif