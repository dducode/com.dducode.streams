using System;
using Streams.StreamTasks.Internal;

namespace Streams.StreamActions {

  internal sealed class AsyncAction : StreamActionBase, IInitializable {

    private readonly Func<CashedTask> _action;
    private CashedTask _task;

    internal AsyncAction(Func<CashedTask> action, StreamToken cancellationToken) : base(cancellationToken) {
      _action = action;
    }

    public void Initialize() {
      _task = _action();
    }

    public override bool Invoke(float deltaTime) {
      if (!base.Invoke(deltaTime))
        return false;

      if (_task.IsCompleted)
        _task.Restart();

      return true;
    }

  }

}