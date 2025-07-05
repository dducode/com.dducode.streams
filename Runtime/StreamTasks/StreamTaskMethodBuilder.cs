using System;
using System.Runtime.CompilerServices;
using JetBrains.Annotations;
using Streams.StreamTasks.TaskSources;

namespace Streams.StreamTasks {

  [UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
  public struct StreamTaskMethodBuilder {

    public StreamTask Task => _source?.Task ?? StreamTask.CompletedTask;
    private StreamTaskSource _source;
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
      _source?.SetResult();
    }

    public void AwaitOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine)
      where TAwaiter : INotifyCompletion
      where TStateMachine : IAsyncStateMachine {
      _source ??= TaskSourcePool.Get<StreamTaskSource>();
      _stateMachineMoveNext ??= stateMachine.MoveNext;
      awaiter.OnCompleted(_stateMachineMoveNext);
    }

    public void AwaitUnsafeOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine)
      where TAwaiter : ICriticalNotifyCompletion
      where TStateMachine : IAsyncStateMachine {
      _source ??= TaskSourcePool.Get<StreamTaskSource>();
      _stateMachineMoveNext ??= stateMachine.MoveNext;
      awaiter.UnsafeOnCompleted(_stateMachineMoveNext);
    }

    public void SetException(Exception exception) {
      if (_source == null)
        throw exception;

      _source.SetException(exception);
    }

  }

  [UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
  public struct StreamTaskMethodBuilder<TResult> {

    public StreamTask<TResult> Task => _source.Task;
    private StreamTaskSource<TResult> _source;
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
      _source ??= TaskSourcePool.Get<StreamTaskSource<TResult>>();
      _source.SetResult(result);
    }

    public void AwaitOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine)
      where TAwaiter : INotifyCompletion
      where TStateMachine : IAsyncStateMachine {
      _source ??= TaskSourcePool.Get<StreamTaskSource<TResult>>();
      _stateMachineMoveNext ??= stateMachine.MoveNext;
      awaiter.OnCompleted(_stateMachineMoveNext);
    }

    public void AwaitUnsafeOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine)
      where TAwaiter : ICriticalNotifyCompletion
      where TStateMachine : IAsyncStateMachine {
      _source ??= TaskSourcePool.Get<StreamTaskSource<TResult>>();
      _stateMachineMoveNext ??= stateMachine.MoveNext;
      awaiter.UnsafeOnCompleted(_stateMachineMoveNext);
    }

    public void SetException(Exception exception) {
      if (_source == null)
        throw exception;

      _source.SetException(exception);
    }

  }

}