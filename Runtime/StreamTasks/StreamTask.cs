using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using JetBrains.Annotations;
using StreamsForUnity.StreamTasks.Internal;

namespace StreamsForUnity.StreamTasks {

  [AsyncMethodBuilder(typeof(StreamTaskMethodBuilder))]
  public partial class StreamTask {

    public static StreamTask CompletedTask { get; } = new() { IsCompleted = true };
    public bool IsCompleted { get; private set; }

    internal Exception Error { get; private set; }
    private readonly Queue<Action> _continuations = new();

    internal StreamTask() {
    }

    public StreamTaskAwaiter GetAwaiter() {
      return new StreamTaskAwaiter(this);
    }

    public StreamTask ContinueWith([NotNull] Action continuation) {
      if (continuation == null)
        throw new ArgumentNullException(nameof(continuation));

      if (IsCompleted) {
        continuation();
        return this;
      }

      _continuations.Enqueue(() => {
        continuation();
        StreamTaskScheduler.RunNext(this);
      });
      var nextTask = new StreamTask();
      StreamTaskScheduler.Schedule(this, nextTask);
      return nextTask;
    }

    public StreamTask ContinueWith([NotNull] Func<StreamTask> continuation) {
      if (continuation == null)
        throw new ArgumentNullException(nameof(continuation));

      if (IsCompleted)
        continuation().ContinueWith(() => StreamTaskScheduler.RunNext(this));
      else
        _continuations.Enqueue(() => continuation().ContinueWith(() => StreamTaskScheduler.RunNext(this)));

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
      if (Error != null)
        return;

      while (_continuations.TryDequeue(out Action continuation))
        continuation();
    }

  }

}