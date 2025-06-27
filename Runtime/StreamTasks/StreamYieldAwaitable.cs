using System;
using System.Runtime.CompilerServices;

namespace Streams.StreamTasks {

  public readonly struct StreamYieldAwaitable : ICriticalNotifyCompletion {

    public bool IsCompleted => false;
    private readonly ExecutionStream _stream;

    public StreamYieldAwaitable(ExecutionStream stream) {
      _stream = stream;
    }

    public StreamYieldAwaitable GetAwaiter() {
      return this;
    }

    public void OnCompleted(Action continuation) {
      _stream.ScheduleContinuation(continuation);
    }

    public void UnsafeOnCompleted(Action continuation) {
      _stream.ScheduleContinuation(continuation);
    }

    public void GetResult() {
    }

  }

}