using System;
using System.Runtime.CompilerServices;
using JetBrains.Annotations;

namespace Streams.StreamTasks.Internal {

  [UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
  public struct RestartableStreamTaskMethodBuilder {

    public RestartableStreamTask Task { get; private set; }
    private IAsyncStateMachineRunner StateMachineRunner { get; set; }
    private Action _stateMachineMoveNext;

    public static RestartableStreamTaskMethodBuilder Create() {
      return new RestartableStreamTaskMethodBuilder();
    }

    public void Start<TStateMachine>(ref TStateMachine stateMachine) where TStateMachine : IAsyncStateMachine {
      stateMachine.MoveNext();
      Task = new RestartableStreamTask { StateMachine = stateMachine };
    }

    public void SetStateMachine(IAsyncStateMachine stateMachine) {
    }

    public void SetResult() {
      Task.SetResult();
    }

    public void AwaitOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine)
      where TAwaiter : INotifyCompletion where TStateMachine : IAsyncStateMachine {
      StateMachineRunner ??= new AsyncStateMachineRunner<TStateMachine>(stateMachine);
      _stateMachineMoveNext ??= StateMachineRunner.Run;
      awaiter.OnCompleted(_stateMachineMoveNext);
    }

    public void AwaitUnsafeOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine)
      where TAwaiter : ICriticalNotifyCompletion where TStateMachine : IAsyncStateMachine {
      StateMachineRunner ??= new AsyncStateMachineRunner<TStateMachine>(stateMachine);
      _stateMachineMoveNext ??= StateMachineRunner.Run;
      awaiter.UnsafeOnCompleted(_stateMachineMoveNext);
    }

    public void SetException(Exception exception) {
      Task.SetException(exception);
    }

  }

}