using System;
using Streams.Exceptions;
using Streams.StreamTasks;
using Streams.StreamTasks.Internal;
using Streams.StreamTasks.TaskSources;
using UnityEngine.Networking;

namespace Streams.Extensions {

  public static class UnityWebRequestExtensions {

    public static StreamTask<UnityWebRequest>.Awaiter GetAwaiter(this UnityWebRequestAsyncOperation operation) {
      return operation.ToStreamTask(StreamToken.None).GetAwaiter();
    }

    public static StreamTask<UnityWebRequest> ToStreamTask(this UnityWebRequestAsyncOperation operation, StreamToken cancellationToken) {
      if (operation.isDone) {
        return operation.webRequest.result switch {
          UnityWebRequest.Result.InProgress =>
            throw new InvalidOperationException("UnityWebRequest is still in progress"),
          UnityWebRequest.Result.Success =>
            StreamTask.FromResult(operation.webRequest),
          UnityWebRequest.Result.ConnectionError =>
            StreamTask.FromException<UnityWebRequest>(new UnityWebRequestConnectionException(operation.webRequest)),
          UnityWebRequest.Result.ProtocolError =>
            StreamTask.FromException<UnityWebRequest>(new UnityWebRequestProtocolException(operation.webRequest)),
          UnityWebRequest.Result.DataProcessingError =>
            StreamTask.FromException<UnityWebRequest>(new UnityWebRequestDataProcessingException(operation.webRequest)),
          _ => throw new ArgumentOutOfRangeException()
        };
      }

      var source = Pool.Get<UnityWebRequestTaskSource>();
      source.Setup(operation);
      source.SetCancellation(cancellationToken);
      StreamTaskHelper.GetRunningStream().AddInvokableTaskSource(source);
      return source.Task;
    }

  }

}