using System;
using Streams.StreamActions.Components;
using Streams.StreamTasks;

namespace Streams.StreamActions {

  internal sealed class AsyncOnceAction : StreamActionBase, ICallbackCompletable {

    public bool IsCompleted => _completion.IsCompleted;

    private readonly Func<StreamTask> _action;
    private readonly Completion _completion = new();
    private StreamTask? _task;

    internal AsyncOnceAction(Func<StreamTask> action, StreamToken cancellationToken) : base(cancellationToken) {
      _action = action;
    }

    public void OnComplete(Action onComplete, StreamToken subscriptionToken = default) {
      _completion.OnComplete(onComplete, subscriptionToken);
    }

    public override void Invoke(float deltaTime) {
      base.Invoke(deltaTime);

      _task ??= _action();

      if (!_task.Value.GetAwaiter().IsCompleted)
        return;

      _task.Value.GetAwaiter().GetResult();
      _completion.Complete();
    }

  }

}