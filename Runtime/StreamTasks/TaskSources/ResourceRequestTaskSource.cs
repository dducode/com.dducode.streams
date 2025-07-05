using UnityEngine;

namespace Streams.StreamTasks.TaskSources {

  internal class ResourceRequestTaskSource : RunnableTaskSource<ResourceRequest, Object> {

    private ResourceRequest _request;

    public override void Setup(ResourceRequest value) {
      _request = value;
    }

    public override bool Invoke(float deltaTime) {
      if (!base.Invoke(deltaTime))
        return false;

      if (!_request.isDone)
        return true;

      SetResult(_request.asset);
      return false;
    }

    private protected override void Reset() {
      base.Reset();
      _request = null;
    }

  }

}