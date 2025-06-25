using System;
using System.Runtime.CompilerServices;

namespace Streams.StreamTasks {

  public readonly struct StreamTaskAwaiter : ICriticalNotifyCompletion {

    public bool IsCompleted => _task.IsCompleted;
    private readonly StreamTask _task;

    public StreamTaskAwaiter(StreamTask task) {
      _task = task;
    }

    public void OnCompleted(Action continuation) {
      _task.ContinueWith(continuation);
    }

    public void UnsafeOnCompleted(Action continuation) {
      _task.ContinueWith(continuation);
    }

    public void GetResult() {
      if (_task.Error != null)
        throw _task.Error;
    }

  }

  public readonly struct StreamTaskAwaiter<TResult> : ICriticalNotifyCompletion {

    public bool IsCompleted => _task.IsCompleted;
    private readonly StreamTask<TResult> _task;

    public StreamTaskAwaiter(StreamTask<TResult> task) {
      _task = task;
    }

    public void OnCompleted(Action continuation) {
      _task.ContinueWith(_ => continuation());
    }

    public void UnsafeOnCompleted(Action continuation) {
      _task.ContinueWith(_ => continuation());
    }

    public TResult GetResult() {
      if (_task.Error != null)
        throw _task.Error;
      return _task.Result;
    }

  }

}