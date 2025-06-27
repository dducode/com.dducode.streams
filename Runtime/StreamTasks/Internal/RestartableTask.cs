using System.Runtime.CompilerServices;

namespace Streams.StreamTasks.Internal {

  [AsyncMethodBuilder(typeof(RestartableTaskMethodBuilder))]
  public sealed class RestartableTask : StreamTask {

    internal IAsyncStateMachine StateMachine { get; set; }

    internal RestartableTask() {
    }

    internal void Restart() {
      if (Error != null)
        throw Error;
      IsCompleted = false;
      StateMachine.MoveNext();
    }

  }

}