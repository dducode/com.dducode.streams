using Streams.StreamTasks;
using Streams.StreamTasks.Internal;
using Streams.StreamTasks.TaskSources;
using UnityEngine.Networking;

namespace Streams.Extensions {

  public static class UnityWebRequestExtensions {

    public static StreamTaskAwaiter<UnityWebRequest> GetAwaiter(this UnityWebRequestAsyncOperation operation) {
      if (!TaskSourcePool.TryGet(out UnityWebRequestTaskSource source))
        source = new UnityWebRequestTaskSource();

      source.Setup(operation);
      StreamTaskHelper.GetRunningStream().AddInvokableTaskSource(source);
      return source.Task.GetAwaiter();
    }

  }

}