using System.Runtime.CompilerServices;

namespace Streams.StreamTasks.Internal {

  internal struct AsyncStateMachineRunner<TStateMachine> : IAsyncStateMachineRunner where TStateMachine : IAsyncStateMachine {

    private TStateMachine _stateMachine;

    public AsyncStateMachineRunner(TStateMachine stateMachine) {
      _stateMachine = stateMachine;
    }

    public void Run() {
      _stateMachine.MoveNext();
    }

  }

}