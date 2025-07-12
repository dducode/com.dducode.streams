#if STREAMS_UNITASK_INTEGRATION
using System;
using Cysharp.Threading.Tasks;
using Streams.StreamTasks.TaskSources;

namespace Streams.Extensions {

  internal class UniTaskContinuationSource : RunnableTaskSource<UniTask> {

    private UniTask _parentTask;

    public override void Setup(UniTask value) {
      _parentTask = value;
    }

    public override bool Invoke(float deltaTime) {
      if (_parentTask.Status == UniTaskStatus.Pending)
        return true;

      try {
        _parentTask.GetAwaiter().GetResult();
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
      _parentTask = UniTask.CompletedTask;
    }

  }

  internal class UniTaskContinuationSource<TResult> : RunnableTaskSource<UniTask<TResult>, TResult> {

    private UniTask<TResult> _parentTask;

    public override void Setup(UniTask<TResult> value) {
      _parentTask = value;
    }

    public override bool Invoke(float deltaTime) {
      if (_parentTask.Status == UniTaskStatus.Pending)
        return true;

      try {
        SetResult(_parentTask.GetAwaiter().GetResult());
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
      _parentTask = UniTask.FromResult(default(TResult));
    }

  }

}
#endif