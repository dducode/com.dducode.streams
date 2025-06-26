using System;
using Streams.StreamTasks.Internal;

namespace Streams.StreamActions {

  public class AsyncAction : SelfClosingAction<AsyncAction, RestartableStreamTask> {

    public override float DeltaTime => _deltaTime;

    private RestartableStreamTask _task;
    private float _deltaTime;

    internal AsyncAction(Func<AsyncAction, RestartableStreamTask> action, StreamToken cancellationToken) : base(action, cancellationToken) {
    }

    internal override void Invoke(float deltaTime) {
      if (Canceled()) {
        _task?.SetCanceled();
        return;
      }

      _deltaTime = deltaTime;
      _task ??= InvokeAction();

      if (!_task.IsCompleted)
        return;

      if (_task.Error != null)
        throw _task.Error;

      _task.Restart();
    }

  }

}