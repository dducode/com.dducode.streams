using System;
using System.Runtime.CompilerServices;
using JetBrains.Annotations;
using Streams.StreamTasks.Internal;

namespace Streams.StreamTasks {

  [UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
  public struct StreamTaskMethodBuilder {

    public StreamTask Task { get; private set; }
    internal IAsyncStateMachineRunner StateMachineRunner { get; private set; }
    private Action _stateMachineMoveNext;

    public static StreamTaskMethodBuilder Create() {
      return new StreamTaskMethodBuilder();
    }

    public void Start<TStateMachine>(ref TStateMachine stateMachine) where TStateMachine : IAsyncStateMachine {
      stateMachine.MoveNext();
    }

    public void SetStateMachine(IAsyncStateMachine stateMachine) {
    }

    public void SetResult() {
      if (Task == null) {
        Task = StreamTask.CompletedTask;
        return;
      }

      Task.SetResult();
    }

    public void AwaitOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine)
      where TAwaiter : INotifyCompletion where TStateMachine : IAsyncStateMachine {
      Task ??= new StreamTask();
      StateMachineRunner ??= new AsyncStateMachineRunner<TStateMachine>(stateMachine);
      _stateMachineMoveNext ??= StateMachineRunner.Run;
      awaiter.OnCompleted(_stateMachineMoveNext);
    }

    public void AwaitUnsafeOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine)
      where TAwaiter : ICriticalNotifyCompletion where TStateMachine : IAsyncStateMachine {
      Task ??= new StreamTask();
      StateMachineRunner ??= new AsyncStateMachineRunner<TStateMachine>(stateMachine);
      _stateMachineMoveNext ??= StateMachineRunner.Run;
      awaiter.UnsafeOnCompleted(_stateMachineMoveNext);
    }

    public void SetException(Exception exception) {
      if (Task == null)
        throw exception;

      Task.SetException(exception);
    }

  }

  [UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
  public struct StreamTaskMethodBuilder<TResult> {

    public StreamTask<TResult> Task { get; private set; }
    internal IAsyncStateMachineRunner StateMachineRunner { get; private set; }
    private Action _stateMachineMoveNext;

    public static StreamTaskMethodBuilder<TResult> Create() {
      return new StreamTaskMethodBuilder<TResult>();
    }

    public void Start<TStateMachine>(ref TStateMachine stateMachine) where TStateMachine : IAsyncStateMachine {
      stateMachine.MoveNext();
    }

    public void SetStateMachine(IAsyncStateMachine stateMachine) {
    }

    public void SetResult(TResult result) {
      if (Task == null) {
        Task = StreamTask.FromResult(result);
        return;
      }

      Task.SetResult(result);
    }

    public void AwaitOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine)
      where TAwaiter : INotifyCompletion where TStateMachine : IAsyncStateMachine {
      Task ??= new StreamTask<TResult>();
      StateMachineRunner ??= new AsyncStateMachineRunner<TStateMachine>(stateMachine);
      _stateMachineMoveNext ??= StateMachineRunner.Run;
      awaiter.OnCompleted(_stateMachineMoveNext);
    }

    public void AwaitUnsafeOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine)
      where TAwaiter : ICriticalNotifyCompletion where TStateMachine : IAsyncStateMachine {
      Task ??= new StreamTask<TResult>();
      StateMachineRunner ??= new AsyncStateMachineRunner<TStateMachine>(stateMachine);
      _stateMachineMoveNext ??= StateMachineRunner.Run;
      awaiter.UnsafeOnCompleted(_stateMachineMoveNext);
    }

    public void SetException(Exception exception) {
      if (Task == null)
        throw exception;

      Task.SetException(exception);
    }

  }

}