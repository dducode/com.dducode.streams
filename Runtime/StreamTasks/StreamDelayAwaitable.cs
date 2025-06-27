using System;
using System.Runtime.CompilerServices;

namespace Streams.StreamTasks {

  public readonly struct StreamDelayAwaitable : ICriticalNotifyCompletion {

    public bool IsCompleted { get; }
    internal static StreamDelayAwaitable ZeroDelay => new(isCompleted: true);

    private readonly ExecutionStream _stream;
    private readonly int _delay;

    public StreamDelayAwaitable(ExecutionStream stream, int delay) {
      IsCompleted = false;
      _stream = stream;
      _delay = delay;
    }

    private StreamDelayAwaitable(bool isCompleted) : this() {
      IsCompleted = isCompleted;
    }

    public StreamDelayAwaitable GetAwaiter() {
      return this;
    }

    public void OnCompleted(Action continuation) {
      _stream.ScheduleDelayContinuation(_delay / 1000f, continuation);
    }

    public void UnsafeOnCompleted(Action continuation) {
      _stream.ScheduleDelayContinuation(_delay / 1000f, continuation);
    }

    public void GetResult() {
    }

  }

}