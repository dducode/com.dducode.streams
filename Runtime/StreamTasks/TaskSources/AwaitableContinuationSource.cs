#if UNITY_2023_1_OR_NEWER
using System;
using UnityEngine;

namespace Streams.StreamTasks.TaskSources {

  public class AwaitableContinuationSource : RunnableTaskSource<Awaitable> {

    private Awaitable _awaitable;

    public override void Setup(Awaitable value) {
      _awaitable = value;
    }

    public override void Invoke(float deltaTime) {
      if (IsCompleted)
        return;

      if (CancellationToken.Released) {
        SetCanceled();
        return;
      }

      if (!_awaitable.GetAwaiter().IsCompleted)
        return;

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
    }

    public override void Reset() {
      base.Reset();
      _awaitable = null;
    }

  }

  public class AwaitableContinuationSource<TResult> : RunnableTaskSource<Awaitable<TResult>, TResult> {

    private Awaitable<TResult> _awaitable;

    public override void Setup(Awaitable<TResult> value) {
      _awaitable = value;
    }

    public override void Invoke(float deltaTime) {
      if (IsCompleted)
        return;

      if (CancellationToken.Released) {
        SetCanceled();
        return;
      }

      if (!_awaitable.GetAwaiter().IsCompleted)
        return;

      try {
        SetResult(_awaitable.GetAwaiter().GetResult());
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
      _awaitable = null;
    }

  }

}
#endif