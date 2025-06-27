using Cysharp.Threading.Tasks;
using Streams.StreamTasks;
using Streams.StreamTasks.Internal;
using UnityEngine.Networking;

namespace Streams.Extensions {

  public static class UnityWebRequestExtensions {

    public static StreamTaskAwaiter<UnityWebRequest> GetAwaiter(this UnityWebRequestAsyncOperation operation) {
      var task = new StreamTask<UnityWebRequest>();
      ExecutionStream runningStream = StreamTaskHelper.GetRunningStream();
      operation.completed += _ => {
        if (operation.webRequest.result is not UnityWebRequest.Result.Success) {
          runningStream.AddOnce(() => task.SetException(new UnityWebRequestException(operation.webRequest)));
          return;
        }

        runningStream.AddOnce(() => task.SetResult(operation.webRequest));
      };

      return task.GetAwaiter();
    }

  }

}