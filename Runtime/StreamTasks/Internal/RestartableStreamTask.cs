using System.Runtime.CompilerServices;

namespace Streams.StreamTasks.Internal {

  [AsyncMethodBuilder(typeof(RestartableStreamTaskMethodBuilder))]
  public sealed class RestartableStreamTask : StreamTask {

    internal IAsyncStateMachine StateMachine { get; set; }

    internal RestartableStreamTask() {
    }

    internal void Restart() {
      if (Error != null)
        throw Error;
      IsCompleted = false;
      StateMachine.MoveNext();
    }

  }

}