using System;
using System.Runtime.CompilerServices;
using Streams.StreamTasks;

namespace Streams.StreamActions {

  public class AsyncAction : SelfClosingAction<AsyncAction, StreamTask> {

    public override float DeltaTime => _deltaTime;

    private StreamTask _task;
    private IAsyncStateMachine _stateMachine;
    private float _deltaTime;

    internal AsyncAction(Func<AsyncAction, StreamTask> action, StreamToken cancellationToken) : base(action, cancellationToken) {
    }

    internal override void Invoke(float deltaTime) {
      if (Canceled()) {
        _task?.SetCanceled();
        return;
      }

      _deltaTime = deltaTime;
      _task ??= InvokeAction();
      _stateMachine ??= _task.GetStateMachine();

      if (!_task.IsCompleted)
        return;

      if (_task.Error != null)
        throw _task.Error;

      _task.Reset();
      _stateMachine.MoveNext();
    }

  }

}