using Streams.StreamTasks;
using Streams.StreamTasks.Internal;
using Streams.StreamTasks.TaskSources;
using UnityEngine;

namespace Streams.Extensions {

  public static class ResourceRequestExtensions {

    public static StreamTask<Object>.Awaiter GetAwaiter(this ResourceRequest request) {
      return request.ToStreamTask(StreamToken.None).GetAwaiter();
    }

    public static StreamTask<Object> ToStreamTask(this ResourceRequest request, StreamToken cancellationToken) {
      if (cancellationToken.Released)
        return StreamTask.FromCanceled<Object>();
      if (request.isDone)
        return StreamTask.FromResult(request.asset);

      var source = Pool.Get<ResourceRequestTaskSource>();
      source.Setup(request);
      source.SetCancellation(cancellationToken);
      StreamTaskHelper.GetRunningStream().AddInvokableTaskSource(source);
      return source.Task;
    }

  }

}