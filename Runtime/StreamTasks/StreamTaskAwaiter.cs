using System;
using System.Runtime.CompilerServices;

namespace StreamsForUnity.StreamTasks {

  public readonly struct StreamTaskAwaiter : ICriticalNotifyCompletion {

    private readonly StreamTask _task;

    public StreamTaskAwaiter(StreamTask task) {
      _task = task;
    }

    public bool IsCompleted => _task.IsCompleted;


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

}