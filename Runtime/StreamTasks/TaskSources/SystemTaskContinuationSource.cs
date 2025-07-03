using System;
using System.Threading.Tasks;

namespace Streams.StreamTasks.TaskSources {

  internal sealed class SystemTaskContinuationSource : RunnableTaskSource<Task> {

    private Task _parentTask;

    public override void Setup(Task value) {
      _parentTask = value;
    }

    public override void Invoke(float deltaTime) {
      if (IsCompleted)
        return;

      if (CancellationToken.Released) {
        SetCanceled();
        return;
      }

      if (!_parentTask.IsCompleted)
        return;

      try {
        _parentTask.GetAwaiter().GetResult();
        SetResult();
      }
      catch (OperationCanceledException) {
        SetCanceled();
      }
      catch (Exception exception) {
        SetException(exception);
      }
    }

    public override void Reset() {
      base.Reset();
      _parentTask = null;
    }

  }

  internal sealed class SystemTaskContinuationSource<TResult> : RunnableTaskSource<Task<TResult>, TResult> {

    private Task<TResult> _parentTask;

    public override void Setup(Task<TResult> value) {
      _parentTask = value;
    }

    public override void Invoke(float deltaTime) {
      if (IsCompleted)
        return;

      if (CancellationToken.Released) {
        SetCanceled();
        return;
      }

      if (!_parentTask.IsCompleted)
        return;

      try {
        SetResult(_parentTask.GetAwaiter().GetResult());
      }
      catch (OperationCanceledException) {
        SetCanceled();
      }
      catch (Exception exception) {
        SetException(exception);
      }
    }

    public override void Reset() {
      base.Reset();
      _parentTask = null;
    }

  }

}