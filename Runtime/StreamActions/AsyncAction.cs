using System;
using Streams.StreamTasks.Internal;

namespace Streams.StreamActions {

  public class AsyncAction : StreamActionBase {

    private protected override Delegate Action => _action;

    private readonly Func<RestartableTask> _action;
    private RestartableTask _task;

    internal AsyncAction(Func<RestartableTask> action, StreamToken cancellationToken) : base(cancellationToken) {
      _action = action;
    }

    internal override void Invoke(float deltaTime) {
      base.Invoke(deltaTime);

      _task ??= _action();

      if (_task.IsCompleted)
        _task.Restart();
    }

  }

}