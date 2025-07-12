#if STREAMS_ADDRESSABLES_INTEGRATION
using System;
using Streams.StreamTasks.TaskSources;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace Streams.Extensions {

  internal class AsyncOperationHandleTaskSource<TResult> : RunnableTaskSource<AsyncOperationHandle<TResult>, TResult> {

    private AsyncOperationHandle<TResult> _handle;

    public override void Setup(AsyncOperationHandle<TResult> value) {
      _handle = value;
    }

    public override bool Invoke(float deltaTime) {
      if (!base.Invoke(deltaTime))
        return false;

      if (!_handle.IsDone)
        return true;

      if (_handle.OperationException != null) {
        if (_handle.OperationException is OperationCanceledException)
          SetCanceled();
        else
          SetException(_handle.OperationException);
      }
      else {
        SetResult(_handle.Result);
      }

      return false;
    }

    private protected override void Reset() {
      base.Reset();
      _handle = default;
    }

  }

}
#endif