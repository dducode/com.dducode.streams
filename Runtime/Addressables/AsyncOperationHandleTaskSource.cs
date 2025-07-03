#if STREAMS_ADDRESSABLES_INTEGRATION
using System;
using Streams.StreamTasks.TaskSources;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace Streams.Addressables {

  public class AsyncOperationHandleTaskSource<TResult> : RunnableTaskSource<AsyncOperationHandle<TResult>, TResult> {

    private AsyncOperationHandle<TResult> _handle;

    public override void Setup(AsyncOperationHandle<TResult> value) {
      _handle = value;
    }

    public override void Invoke(float deltaTime) {
      if (IsCompleted)
        return;

      if (CancellationToken.Released) {
        SetCanceled();
        return;
      }

      if (!_handle.IsDone)
        return;

      try {
        SetResult(_handle.Result);
      }
      catch (OperationCanceledException) {
        SetCanceled();
      }
      catch (Exception e) {
        SetException(e);
      }
    }

    public override void Reset() {
      base.Reset();
      _handle = default;
    }

  }

}
#endif