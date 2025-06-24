using System;
using System.Threading;
using Streams.StreamTasks;

namespace Streams.StreamActions {

  public class AsyncAction : SelfClosingAction<AsyncAction, StreamTask> {

    public override float DeltaTime => _deltaTime;

    private StreamTask _task;
    private float _deltaTime;

    internal AsyncAction(Func<AsyncAction, StreamTask> action, CancellationToken cancellationToken) : base(action, cancellationToken) {
      _task = StreamTask.CompletedTask;
    }

    internal override void Invoke(float deltaTime) {
      if (Canceled() && _task != StreamTask.CompletedTask) {
        _task.SetCanceled();
        return;
      }

      _deltaTime = deltaTime;

      if (!_task.IsCompleted)
        return;

      if (_task.Error != null)
        throw _task.Error;

      _task = InvokeAction();
    }

  }

}