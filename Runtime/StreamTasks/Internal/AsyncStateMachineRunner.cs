using System.Runtime.CompilerServices;

namespace StreamsForUnity.StreamTasks.Internal {

  internal class AsyncStateMachineRunner<TStateMachine> : IAsyncStateMachineRunner where TStateMachine : IAsyncStateMachine {

    private readonly TStateMachine _stateMachine;
    private readonly StreamTask _task;

    internal AsyncStateMachineRunner(TStateMachine stateMachine, StreamTask task) {
      _stateMachine = stateMachine;
      _task = task;
    }

    public void Run() {
      if (_task.Error != null)
        return;
      _stateMachine.MoveNext();
    }

  }

}