using System;
using System.Runtime.CompilerServices;

namespace Streams.StreamTasks.Internal {

  [AsyncMethodBuilder(typeof(RestartableTaskMethodBuilder))]
  public sealed class CashedTask {

    internal bool IsCompleted { get; private set; }
    internal IAsyncStateMachine StateMachine { get; set; }

    private Exception _error;

    internal CashedTask() {
    }

    internal void Restart() {
      if (_error != null)
        throw _error;
      IsCompleted = false;
      StateMachine.MoveNext();
    }

    public void SetException(Exception exception) {
      IsCompleted = true;
      _error = exception;
    }

    public void SetResult() {
      IsCompleted = true;
    }

  }

}