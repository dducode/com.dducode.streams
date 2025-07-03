using System.Runtime.CompilerServices;

namespace Streams.StreamTasks {

  public struct StateMachineRunner<TStateMachine> where TStateMachine : IAsyncStateMachine {

    private TStateMachine _stateMachine;

    public StateMachineRunner(TStateMachine stateMachine) {
      _stateMachine = stateMachine;
    }

    public void Run() {
      _stateMachine.MoveNext();
    }

  }

}