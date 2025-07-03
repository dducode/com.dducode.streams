#if STREAMS_UNITASK_INTEGRATION
using System;
using Cysharp.Threading.Tasks;
using Streams.StreamTasks;
using Streams.StreamTasks.TaskSources;

namespace Streams.UniTasks {

  public class UniTaskContinuationSource : RunnableTaskSource<UniTask> {

    private UniTask _parentTask;

    public override void Setup(UniTask value) {
      _parentTask = value;
    }

    public override void Invoke(float deltaTime) {
      if (IsCompleted)
        return;

      if (CancellationToken.Released) {
        SetCanceled();
        return;
      }

      if (_parentTask.Status == UniTaskStatus.Pending)
        return;

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
    }

    public override void Reset() {
      base.Reset();
      _parentTask = UniTask.CompletedTask;
    }

  }

  public class UniTaskContinuationSource<TResult> : RunnableTaskSource<UniTask<TResult>, TResult> {

    private UniTask<TResult> _parentTask;

    public override void Setup(UniTask<TResult> value) {
      _parentTask = value;
    }

    public override void Invoke(float deltaTime) {
      if (IsCompleted)
        return;

      if (CancellationToken.Released) {
        SetCanceled();
        return;
      }

      if (_parentTask.Status == UniTaskStatus.Pending)
        return;

      try {
        SetResult(_parentTask.GetAwaiter().GetResult());
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
      _parentTask = UniTask.FromResult(default(TResult));
    }

  }

}
#endif