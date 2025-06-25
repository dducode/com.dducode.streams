using System;
using System.Runtime.CompilerServices;

namespace Streams.StreamTasks {

  public struct StreamYieldAwaitable : ICriticalNotifyCompletion {

    public bool IsCompleted => false;
    private ExecutionStream _stream;

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

    internal void SetStream(ExecutionStream stream) {
      _stream = stream;
    }

  }

}