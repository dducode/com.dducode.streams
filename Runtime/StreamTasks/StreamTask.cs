using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;

namespace StreamsForUnity.StreamTasks {

  [AsyncMethodBuilder(typeof(StreamTaskMethodBuilder))]
  public class StreamTask {

    public static StreamTask CompletedTask { get; } = new() { IsCompleted = true };

    public bool IsCompleted { get; private set; }

    internal Exception Error { get; private set; }
    private Queue<Action> _continuations = new();

    public static StreamTask Yield(CancellationToken token = default) {
      var task = new StreamTask();
      GetExecutableStream().AddOnce(task.SetResult, token);
      token.Register(task.SetCanceled);
      return task;
    }

    public static StreamTask Delay(uint milliseconds, CancellationToken token = default) {
      var task = new StreamTask();
      GetExecutableStream().AddTimer(milliseconds / 1000f, task.SetResult, token);
      token.Register(task.SetCanceled);
      return task;
    }

    public static StreamTask WaitWhile(Func<bool> condition, CancellationToken token = default) {
      var task = new StreamTask();
      var cts = new CancellationTokenSource();
      GetExecutableStream().Add(_ => {
        if (condition())
          return;

        task.SetResult();
        cts.Cancel();
      }, cts.Token);

      token.Register(() => {
        task.SetCanceled();
        cts.Cancel();
      });
      return task;
    }

    internal StreamTask() {
    }

    public StreamTaskAwaiter GetAwaiter() {
      return new StreamTaskAwaiter(this);
    }

    public StreamTask ContinueWith(Action continuation) {
      if (IsCompleted)
        continuation();
      else
        _continuations.Enqueue(continuation);

      return this;
    }

    public StreamTask ContinueWith(Func<StreamTask> continuation) {
      if (IsCompleted)
        continuation();
      else
        _continuations.Enqueue(() => continuation());

      return this;
    }

    internal void SetResult() {
      Complete();
    }

    internal void SetCanceled() {
      Complete(new OperationCanceledException());
    }

    private static ExecutionStream GetExecutableStream() {
      return Streams.ExecutableStream ?? throw new StreamsException("Cannot use stream tasks outside of stream execution");
    }

    private void Complete(Exception error = null) {
      if (IsCompleted)
        return;

      IsCompleted = true;
      Error = error;
      while (_continuations.TryDequeue(out Action continuation))
        continuation?.Invoke();
    }

  }

}