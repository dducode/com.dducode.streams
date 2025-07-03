using Cysharp.Threading.Tasks;
using Streams.StreamTasks;
using Streams.StreamTasks.TaskSources;
using UnityEngine.Networking;

namespace Streams.Extensions {

  public class UnityWebRequestTaskSource : RunnableTaskSource<UnityWebRequestAsyncOperation, UnityWebRequest> {

    private UnityWebRequestAsyncOperation _operation;

    public override void Setup(UnityWebRequestAsyncOperation value) {
      _operation = value;
    }

    public override void Invoke(float deltaTime) {
      if (IsCompleted)
        return;

      if (CancellationToken.Released) {
        SetCanceled();
        return;
      }

      if (!_operation.isDone)
        return;

      if (_operation.webRequest.result is not UnityWebRequest.Result.Success)
        SetException(new UnityWebRequestException(_operation.webRequest));
      else
        SetResult(_operation.webRequest);
    }

    public override void Reset() {
      base.Reset();
      _operation = null;
    }

  }

}