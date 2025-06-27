using System;
using Streams.StreamTasks.Internal;

namespace Streams.StreamActions {

  public class AsyncAction : SelfClosingAction<RestartableTask> {

    public override float DeltaTime => _deltaTime;

    private RestartableTask _task;
    private float _deltaTime;

    internal AsyncAction(Func<SelfClosingAction<RestartableTask>, RestartableTask> action, StreamToken cancellationToken) :
      base(action, cancellationToken) {
    }

    internal override void Invoke(float deltaTime) {
      if (Canceled()) {
        _task?.SetCanceled();
        return;
      }

      if (!CanExecute())
        return;

      _deltaTime = deltaTime;
      _task ??= InvokeAction();

      try {
        if (_task.IsCompleted)
          _task.Restart();
      }
      catch (InterruptException) {
      }
    }

  }

}