using System;
using Streams.StreamActions.Components;
using Streams.StreamTasks;

namespace Streams.StreamActions {

  internal sealed class AsyncOnceAction : StreamActionBase, IInitializable, ICompletable {

    private readonly Func<StreamTask> _action;
    private readonly Completion _completion = new();

    private StreamTask _task;
    private bool _completionRequested;

    internal AsyncOnceAction(Func<StreamTask> action, StreamToken cancellationToken) : base(cancellationToken) {
      _action = action;
    }

    public void Initialize() {
      _task = _action();
      StreamTaskAwaiter awaiter = _task.GetAwaiter();
      if (awaiter.IsCompleted) {
        _completionRequested = true;
        return;
      }

      awaiter.OnCompleted(() => _completionRequested = true);
    }

    public void OnComplete(Action onComplete, StreamToken subscriptionToken = default) {
      _completion.OnComplete(onComplete, subscriptionToken);
    }

    public override bool Invoke(float deltaTime) {
      StreamTaskAwaiter awaiter = _task.GetAwaiter();

      if (!base.Invoke(deltaTime)) {
        awaiter.OnCompleted(awaiter.GetResult);
        return false;
      }

      if (_completionRequested) {
        awaiter.GetResult();
        _completion.Complete();
        return false;
      }

      return true;
    }

  }

}