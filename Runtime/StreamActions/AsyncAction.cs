using System;
using Streams.StreamTasks.Internal;

namespace Streams.StreamActions {

  internal sealed class AsyncAction : StreamActionBase {

    private readonly Func<CashedTask> _action;
    private CashedTask _task;
    private bool _restart;

    internal AsyncAction(Func<CashedTask> action, StreamToken cancellationToken) : base(cancellationToken) {
      _action = action;
    }

    public override bool Invoke(float deltaTime) {
      if (!base.Invoke(deltaTime))
        return false;

      _task ??= _action();

      if (_restart) {
        _task.Restart();
        _restart = false;
      }

      if (_task.IsCompleted)
        _restart = true;

      return true;
    }

  }

}