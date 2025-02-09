using System.Runtime.CompilerServices;

namespace StreamsForUnity.StreamTasks.Internal {

  internal class AsyncStateMachineRunner<TStateMachine> : IAsyncStateMachineRunner where TStateMachine : IAsyncStateMachine {

    private readonly TStateMachine _stateMachine;

    internal AsyncStateMachineRunner(TStateMachine stateMachine) {
      _stateMachine = stateMachine;
    }

    public void Run() {
      _stateMachine.MoveNext();
    }

  }

}