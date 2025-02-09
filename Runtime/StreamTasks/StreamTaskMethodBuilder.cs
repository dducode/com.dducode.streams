using System;
using System.Runtime.CompilerServices;
using JetBrains.Annotations;
using StreamsForUnity.StreamTasks.Internal;
using UnityEngine;

namespace StreamsForUnity.StreamTasks {

  [UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
  public struct StreamTaskMethodBuilder {

    public StreamTask Task { get; private set; }

    private IAsyncStateMachineRunner _runner;

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
      _runner ??= new AsyncStateMachineRunner<TStateMachine>(stateMachine);
      awaiter.OnCompleted(_runner.Run);
    }

    public void AwaitUnsafeOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine)
      where TAwaiter : ICriticalNotifyCompletion where TStateMachine : IAsyncStateMachine {
      Task ??= new StreamTask();
      _runner ??= new AsyncStateMachineRunner<TStateMachine>(stateMachine);
      awaiter.UnsafeOnCompleted(_runner.Run);
    }

    public void SetException(Exception exception) {
      Debug.LogError("An error occurred while executing async method");
      Debug.LogException(exception);
    }

  }

}