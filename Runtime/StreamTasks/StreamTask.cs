using System;
using System.Runtime.CompilerServices;
using StreamsForUnity.StreamTasks.Internal;

namespace StreamsForUnity.StreamTasks {

  [AsyncMethodBuilder(typeof(StreamTaskMethodBuilder))]
  public partial class StreamTask {

    public static StreamTask CompletedTask { get; } = new() { IsCompleted = true };
    public bool IsCompleted { get; private set; }

    internal Exception Error { get; private set; }
    private Action _continuation;

    internal StreamTask() {
    }

    public StreamTaskAwaiter GetAwaiter() {
      return new StreamTaskAwaiter(this);
    }

    public StreamTask ContinueWith(Action continuation) {
      if (IsCompleted) {
        continuation();
        return this;
      }

      if (_continuation != null)
        throw new StreamsException("Cannot set continuation after the previous continuation");

      _continuation = () => {
        continuation();
        StreamTaskScheduler.FireCompleted(this);
      };
      var nextTask = new StreamTask();
      StreamTaskScheduler.Schedule(this, nextTask);
      return nextTask;
    }

    public StreamTask ContinueWith(Func<StreamTask> continuation) {
      if (IsCompleted)
        continuation().ContinueWith(() => StreamTaskScheduler.FireCompleted(this));
      else
        _continuation = () => continuation().ContinueWith(() => StreamTaskScheduler.FireCompleted(this));

      var nextTask = new StreamTask();
      StreamTaskScheduler.Schedule(this, nextTask);
      return nextTask;
    }

    internal void SetResult() {
      Complete();
    }

    internal void SetCanceled() {
      Complete(new OperationCanceledException());
    }

    private void Complete(Exception error = null) {
      if (IsCompleted)
        return;

      IsCompleted = true;
      Error = error;
      _continuation?.Invoke();
    }

  }

}