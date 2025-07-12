using Streams.StreamTasks;
using Streams.StreamTasks.Internal;
using Streams.StreamTasks.TaskSources;
using UnityEngine;

namespace Streams.Extensions {

  public static class AsyncOperationExtensions {

#if !UNITY_2023_1_OR_NEWER
    public static StreamTaskAwaiter GetAwaiter(this AsyncOperation asyncOperation) {
      return asyncOperation.ToStreamTask().GetAwaiter();
    }
#endif

    public static StreamTask ToStreamTask(this AsyncOperation asyncOperation) {
      return asyncOperation.ToStreamTask(StreamToken.None);
    }

    public static StreamTask ToStreamTask(this AsyncOperation asyncOperation, StreamToken cancellationToken) {
      if (cancellationToken.Released)
        return StreamTask.FromCanceled();
      if (asyncOperation.isDone)
        return StreamTask.CompletedTask;

      var source = Pool.Get<AsyncOperationTaskSource>();
      source.Setup(asyncOperation);
      source.SetCancellation(cancellationToken);
      StreamTaskHelper.GetRunningStream().AddInvokableTaskSource(source);
      return source.Task;
    }

  }

}