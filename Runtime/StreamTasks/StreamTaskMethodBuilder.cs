using System;
using System.Runtime.CompilerServices;
using JetBrains.Annotations;
using StreamsForUnity.StreamTasks.Internal;
using UnityEngine;

namespace StreamsForUnity.StreamTasks {

  [UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
  public struct StreamTaskMethodBuilder {

    public StreamTask Task { get; private set; }
    private Action _onCompleted;

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
      _onCompleted ??= new AsyncStateMachineRunner<TStateMachine>(stateMachine).Run;
      awaiter.OnCompleted(_onCompleted);
    }

    public void AwaitUnsafeOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine)
      where TAwaiter : ICriticalNotifyCompletion where TStateMachine : IAsyncStateMachine {
      Task ??= new StreamTask();
      _onCompleted ??= new AsyncStateMachineRunner<TStateMachine>(stateMachine).Run;
      awaiter.UnsafeOnCompleted(_onCompleted);
    }

    public void SetException(Exception exception) {
      Debug.LogError("Unhandled error was occurred");
      Debug.LogException(exception);
    }

  }

}