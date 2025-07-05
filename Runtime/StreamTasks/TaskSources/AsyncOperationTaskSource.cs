using UnityEngine;

namespace Streams.StreamTasks.TaskSources {

  internal sealed class AsyncOperationTaskSource : RunnableTaskSource<AsyncOperation> {

    private AsyncOperation _operation;

    public override void Setup(AsyncOperation value) {
      _operation = value;
    }

    public override bool Invoke(float deltaTime) {
      if (!base.Invoke(deltaTime))
        return false;

      if (!_operation.isDone)
        return true;

      SetResult();
      return false;
    }

    private protected override void Reset() {
      base.Reset();
      _operation = null;
    }

  }

}