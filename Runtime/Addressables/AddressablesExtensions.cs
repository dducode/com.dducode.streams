#if STREAMS_ADDRESSABLES_INTEGRATION
using Streams.StreamTasks;
using Streams.StreamTasks.Internal;
using Streams.StreamTasks.TaskSources;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace Streams.Addressables {

  public static class AddressablesExtensions {

    public static StreamTaskAwaiter<TResult> GetAwaiter<TResult>(this AsyncOperationHandle<TResult> handle) {
      if (TaskSourcePool.TryGet(out AsyncOperationHandleTaskSource<TResult> source))
        source = new AsyncOperationHandleTaskSource<TResult>();

      source.Setup(handle);
      StreamTaskHelper.GetRunningStream().AddInvokableTaskSource(source);
      return source.Task.GetAwaiter();
    }

  }

}
#endif