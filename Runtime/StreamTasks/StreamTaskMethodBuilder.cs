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
      if (_source == null) {
        if (!TaskSourcePool.TryGet(out StreamTaskSource source))
          source = new StreamTaskSource();
        _source = source;
      }

      _stateMachineMoveNext ??= stateMachine.MoveNext;
      awaiter.OnCompleted(_stateMachineMoveNext);
    }

    public void AwaitUnsafeOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine)
      where TAwaiter : ICriticalNotifyCompletion
      where TStateMachine : IAsyncStateMachine {
      if (_source == null) {
        if (!TaskSourcePool.TryGet(out StreamTaskSource source))
          source = new StreamTaskSource();
        _source = source;
      }

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
  public struct ValueStreamTaskMethodBuilder<TResult> {

    public StreamTask<TResult> Task => _source.Task;
    private StreamTaskSource<TResult> _source;
    private Action _stateMachineMoveNext;

    public static ValueStreamTaskMethodBuilder<TResult> Create() {
      return new ValueStreamTaskMethodBuilder<TResult>();
    }

    public void Start<TStateMachine>(ref TStateMachine stateMachine) where TStateMachine : IAsyncStateMachine {
      stateMachine.MoveNext();
    }

    public void SetStateMachine(IAsyncStateMachine stateMachine) {
    }

    public void SetResult(TResult result) {
      if (_source == null) {
        if (!TaskSourcePool.TryGet(out StreamTaskSource<TResult> source))
          source = new StreamTaskSource<TResult>();
        _source = source;
      }

      _source.SetResult(result);
    }

    public void AwaitOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine)
      where TAwaiter : INotifyCompletion
      where TStateMachine : IAsyncStateMachine {
      if (_source == null) {
        if (!TaskSourcePool.TryGet(out StreamTaskSource<TResult> source))
          source = new StreamTaskSource<TResult>();
        _source = source;
      }

      _stateMachineMoveNext ??= stateMachine.MoveNext;
      awaiter.OnCompleted(_stateMachineMoveNext);
    }

    public void AwaitUnsafeOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine)
      where TAwaiter : ICriticalNotifyCompletion
      where TStateMachine : IAsyncStateMachine {
      if (_source == null) {
        if (!TaskSourcePool.TryGet(out StreamTaskSource<TResult> source))
          source = new StreamTaskSource<TResult>();
        _source = source;
      }

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