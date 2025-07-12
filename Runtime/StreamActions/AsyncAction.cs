using System;
using Streams.StreamTasks;

namespace Streams.StreamActions {

  internal sealed class AsyncAction : StreamActionBase {

    private readonly Func<StreamTask> _action;
    private StreamTask.Awaiter _awaiter;
    private bool _restart = true;

    internal AsyncAction(Func<StreamTask> action, StreamToken cancellationToken) : base(cancellationToken) {
      _action = action;
    }

    public override bool Invoke(float deltaTime) {
      if (!base.Invoke(deltaTime)) {
        if (!_awaiter.IsCompleted)
          _awaiter.OnCompleted(_awaiter.GetResult);
        return false;
      }

      if (_restart) {
        _awaiter = _action().GetAwaiter();
        _restart = false;
      }

      if (_awaiter.IsCompleted) {
        _awaiter.GetResult();
        _restart = true;
      }

      return true;
    }

  }

}