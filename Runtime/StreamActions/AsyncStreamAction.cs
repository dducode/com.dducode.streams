using System;
using Streams.StreamActions.Components;
using Streams.StreamTasks;

namespace Streams.StreamActions {

  public class AsyncStreamAction : StreamAction, ICompletable {

    private protected override Delegate Action => _action;

    private readonly Func<StreamTask> _action;
    private readonly Completion _completion = new();
    private StreamTask _task;

    internal AsyncStreamAction(Func<StreamTask> action, StreamToken cancellationToken) : base(cancellationToken, uint.MaxValue) {
      _action = action;
    }

    public void OnComplete(Action onComplete, StreamToken subscriptionToken = default) {
      _completion.OnComplete(onComplete, subscriptionToken);
    }

    internal override void Invoke(float deltaTime) {
      if (Canceled()) {
        _task?.SetCanceled();
        return;
      }

      _task ??= _action();

      if (!_task.IsCompleted)
        return;

      if (_task.Error != null)
        throw _task.Error;

      _completion.Complete();
    }

  }

}