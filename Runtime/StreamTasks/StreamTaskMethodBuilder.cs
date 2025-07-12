using System;
using System.Runtime.CompilerServices;
using JetBrains.Annotations;
using Streams.StreamTasks.TaskSources;

namespace Streams.StreamTasks {

  [UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
  public struct StreamTaskMethodBuilder {

    public StreamTask Task => _source?.Task ?? StreamTask.CompletedTask;

    private StreamTaskSource _source;
    private IAsyncStateMachineRunner _stateMachineRunner;

    public static StreamTaskMethodBuilder Create() {
      return new StreamTaskMethodBuilder();
    }

    public void Start<TStateMachine>(ref TStateMachine stateMachine) where TStateMachine : IAsyncStateMachine {
      stateMachine.MoveNext();
    }

    public void SetStateMachine(IAsyncStateMachine stateMachine) {
    }

    public void SetResult() {
      Complete();
    }

    public void AwaitOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine)
      where TAwaiter : INotifyCompletion
      where TStateMachine : IAsyncStateMachine {
      _stateMachineRunner ??= Pool.Get<AsyncStateMachineRunner<TStateMachine>>().SetStateMachine(stateMachine);
      _source ??= Pool.Get<StreamTaskSource>();
      awaiter.OnCompleted(_stateMachineRunner.MoveNextDelegate);
    }

    public void AwaitUnsafeOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine)
      where TAwaiter : ICriticalNotifyCompletion
      where TStateMachine : IAsyncStateMachine {
      AwaitOnCompleted(ref awaiter, ref stateMachine);
    }

    public void SetException(Exception exception) {
      Complete(exception);
    }

    private void Complete(Exception error = null) {
      try {
        if (_source == null) {
          if (error != null)
            throw error;
        }
        else {
          switch (error) {
            case null:
              _source.SetResult();
              break;
            case OperationCanceledException:
              _source.SetCanceled();
              break;
            default:
              _source.SetException(error);
              break;
          }
        }
      }
      finally {
        if (_stateMachineRunner != null) {
          Pool.Return(_stateMachineRunner);
          _stateMachineRunner = null;
        }
      }
    }

  }

  [UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
  public struct StreamTaskMethodBuilder<TResult> {

    public StreamTask<TResult> Task => _source.Task;

    private StreamTaskSource<TResult> _source;
    private IAsyncStateMachineRunner _stateMachineRunner;

    public static StreamTaskMethodBuilder<TResult> Create() {
      return new StreamTaskMethodBuilder<TResult>();
    }

    public void Start<TStateMachine>(ref TStateMachine stateMachine) where TStateMachine : IAsyncStateMachine {
      stateMachine.MoveNext();
    }

    public void SetStateMachine(IAsyncStateMachine stateMachine) {
    }

    public void SetResult(TResult result) {
      Complete(result);
    }

    public void AwaitOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine)
      where TAwaiter : INotifyCompletion
      where TStateMachine : IAsyncStateMachine {
      _stateMachineRunner ??= Pool.Get<AsyncStateMachineRunner<TStateMachine>>().SetStateMachine(stateMachine);
      _source ??= Pool.Get<StreamTaskSource<TResult>>();
      awaiter.OnCompleted(_stateMachineRunner.MoveNextDelegate);
    }

    public void AwaitUnsafeOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine)
      where TAwaiter : ICriticalNotifyCompletion
      where TStateMachine : IAsyncStateMachine {
      AwaitOnCompleted(ref awaiter, ref stateMachine);
    }

    public void SetException(Exception exception) {
      Complete(default, exception);
    }

    private void Complete(TResult result, Exception error = null) {
      _source ??= Pool.Get<StreamTaskSource<TResult>>();

      try {
        switch (error) {
          case null:
            _source.SetResult(result);
            break;
          case OperationCanceledException:
            _source.SetCanceled();
            break;
          default:
            _source.SetException(error);
            break;
        }
      }
      finally {
        if (_stateMachineRunner != null) {
          Pool.Return(_stateMachineRunner);
          _stateMachineRunner = null;
        }
      }
    }

  }

}