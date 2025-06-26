#if STREAMS_ADDRESSABLES_INTEGRATION
using Streams.StreamTasks;
using Streams.StreamTasks.Internal;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace Streams.Addressables {

  public static class AddressablesExtensions {

    public static StreamTaskAwaiter<TResult> GetAwaiter<TResult>(this AsyncOperationHandle<TResult> handle) {
      var task = new StreamTask<TResult>();
      ExecutionStream runningStream = StreamTaskHelper.GetRunningStream();
      handle.Completed += op => runningStream.AddOnce(() => task.SetResult(op.Result));
      return task.GetAwaiter();
    }

  }

}
#endif