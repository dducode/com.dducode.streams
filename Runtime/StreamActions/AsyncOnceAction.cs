using System;
using Streams.StreamActions.Components;
using Streams.StreamTasks;

namespace Streams.StreamActions {

  internal sealed class AsyncOnceAction : StreamActionBase, ICompletable {

    private readonly Func<StreamTask> _action;
    private readonly Completion _completion = new();

    private StreamTask.Awaiter _awaiter;
    private bool _started;

    internal AsyncOnceAction(Func<StreamTask> action, StreamToken cancellationToken) : base(cancellationToken) {
      _action = action;
    }

    public void OnComplete(Action onComplete, StreamToken subscriptionToken = default) {
      _completion.OnComplete(onComplete, subscriptionToken);
    }

    public override bool Invoke(float deltaTime) {
      if (!base.Invoke(deltaTime)) {
        if (!_awaiter.IsCompleted)
          _awaiter.OnCompleted(_awaiter.GetResult);
        return false;
      }

      if (!_started) {
        _awaiter = _action().GetAwaiter();
        _started = true;
      }

      if (_awaiter.IsCompleted) {
        _awaiter.GetResult();
        _completion.Complete();
        return false;
      }

      return true;
    }

  }

}