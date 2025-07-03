using UnityEngine;

namespace Streams.StreamTasks.TaskSources {

  internal sealed class AsyncOperationTaskSource : RunnableTaskSource<AsyncOperation> {

    private AsyncOperation _operation;

    public override void Setup(AsyncOperation value) {
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

      SetResult();
    }

    public override void Reset() {
      base.Reset();
      _operation = null;
    }

  }

}