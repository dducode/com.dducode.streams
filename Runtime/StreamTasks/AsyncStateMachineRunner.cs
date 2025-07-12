using System;
using System.Runtime.CompilerServices;

namespace Streams.StreamTasks {

  internal interface IAsyncStateMachineRunner {

    public Action MoveNextDelegate { get; }

  }

  internal class AsyncStateMachineRunner<TStateMachine> : IAsyncStateMachineRunner where TStateMachine : IAsyncStateMachine {

    public Action MoveNextDelegate { get; }

    private TStateMachine _stateMachine;

    public AsyncStateMachineRunner() {
      MoveNextDelegate = MoveNext;
    }

    public AsyncStateMachineRunner<TStateMachine> SetStateMachine(TStateMachine stateMachine) {
      _stateMachine = stateMachine;
      return this;
    }

    private void MoveNext() {
      _stateMachine.MoveNext();
    }

  }

}