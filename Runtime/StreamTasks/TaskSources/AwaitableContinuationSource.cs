#if UNITY_2023_1_OR_NEWER
using System;
using UnityEngine;

namespace Streams.StreamTasks.TaskSources {

  internal class AwaitableContinuationSource : RunnableTaskSource<Awaitable> {

    private Awaitable _awaitable;

    public override void Setup(Awaitable value) {
      _awaitable = value;
    }

    public override bool Invoke(float deltaTime) {
      if (!_awaitable.GetAwaiter().IsCompleted)
        return true;

      try {
        _awaitable.GetAwaiter().GetResult();
        SetResult();
      }
      catch (OperationCanceledException) {
        SetCanceled();
      }
      catch (Exception e) {
        SetException(e);
      }

      return false;
    }

    private protected override void Reset() {
      base.Reset();
      _awaitable = null;
    }

  }

  internal class AwaitableContinuationSource<TResult> : RunnableTaskSource<Awaitable<TResult>, TResult> {

    private Awaitable<TResult> _awaitable;

    public override void Setup(Awaitable<TResult> value) {
      _awaitable = value;
    }

    public override bool Invoke(float deltaTime) {
      if (!_awaitable.GetAwaiter().IsCompleted)
        return true;

      try {
        SetResult(_awaitable.GetAwaiter().GetResult());
      }
      catch (OperationCanceledException) {
        SetCanceled();
      }
      catch (Exception e) {
        SetException(e);
      }

      return false;
    }

    private protected override void Reset() {
      base.Reset();
      _awaitable = null;
    }

  }

}
#endif