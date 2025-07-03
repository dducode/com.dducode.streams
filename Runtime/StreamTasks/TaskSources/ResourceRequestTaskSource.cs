using UnityEngine;

namespace Streams.StreamTasks.TaskSources {

  public class ResourceRequestTaskSource : RunnableTaskSource<ResourceRequest, Object> {

    internal ResourceRequest request;

    public override void Setup(ResourceRequest value) {
      request = value;
    }

    public override void Invoke(float deltaTime) {
      if (IsCompleted)
        return;

      if (CancellationToken.Released) {
        SetCanceled();
        return;
      }

      if (!request.isDone)
        return;

      SetResult(request.asset);
    }

    public override void Reset() {
      base.Reset();
      request = null;
    }

  }

}