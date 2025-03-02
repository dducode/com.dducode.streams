using System;
using StreamsForUnity.StreamTasks;

namespace StreamsForUnity.StreamActions {

  public class AsyncStreamAction : StreamAction, ICompletable {

    public event Action OnComplete {
      add => _completion.OnComplete += value;
      remove => _completion.OnComplete -= value;
    }

    private protected override Delegate Action => _action;

    private readonly Func<StreamTask> _action;
    private readonly Completion _completion = new();
    private StreamTask _task;

    internal AsyncStreamAction(Func<StreamTask> action, StreamToken cancellationToken) : base(cancellationToken, uint.MaxValue) {
      _action = action;
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