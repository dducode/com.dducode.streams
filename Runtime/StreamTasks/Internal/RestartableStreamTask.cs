using System.Runtime.CompilerServices;

namespace Streams.StreamTasks.Internal {

  [AsyncMethodBuilder(typeof(RestartableStreamTaskMethodBuilder))]
  public sealed class RestartableStreamTask : StreamTask {

    internal IAsyncStateMachine StateMachine { get; set; }

    internal RestartableStreamTask() {
    }

    internal void Restart() {
      IsCompleted = false;
      Error = null;
      StateMachine.MoveNext();
    }

  }

}