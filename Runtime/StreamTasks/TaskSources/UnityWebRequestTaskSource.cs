using System;
using Streams.Exceptions;
using UnityEngine.Networking;

namespace Streams.StreamTasks.TaskSources {

  internal class UnityWebRequestTaskSource : RunnableTaskSource<UnityWebRequestAsyncOperation, UnityWebRequest> {

    private UnityWebRequestAsyncOperation _operation;

    public override void Setup(UnityWebRequestAsyncOperation value) {
      _operation = value;
    }

    public override bool Invoke(float deltaTime) {
      if (!base.Invoke(deltaTime))
        return false;

      if (!_operation.isDone)
        return true;

      switch (_operation.webRequest.result) {
        case UnityWebRequest.Result.InProgress:
          throw new InvalidOperationException("UnityWebRequest is still in progress");
        case UnityWebRequest.Result.Success:
          SetResult(_operation.webRequest);
          break;
        case UnityWebRequest.Result.ConnectionError:
          SetException(new UnityWebRequestConnectionException(_operation.webRequest));
          break;
        case UnityWebRequest.Result.ProtocolError:
          SetException(new UnityWebRequestProtocolException(_operation.webRequest));
          break;
        case UnityWebRequest.Result.DataProcessingError:
          SetException(new UnityWebRequestDataProcessingException(_operation.webRequest));
          break;
        default:
          throw new ArgumentOutOfRangeException();
      }

      return false;
    }

    private protected override void Reset() {
      base.Reset();
      _operation = null;
    }

  }

}