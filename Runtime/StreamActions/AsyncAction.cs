using System;
using Streams.StreamTasks.Internal;

namespace Streams.StreamActions {

  internal sealed class AsyncAction : StreamActionBase {

    private readonly Func<CashedTask> _action;
    private CashedTask _task;

    internal AsyncAction(Func<CashedTask> action, StreamToken cancellationToken) : base(cancellationToken) {
      _action = action;
    }

    public override void Invoke(float deltaTime) {
      base.Invoke(deltaTime);

      _task ??= _action();

      if (_task.IsCompleted)
        _task.Restart();
    }

  }

}