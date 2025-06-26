using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using JetBrains.Annotations;

namespace Streams.StreamTasks {

  [AsyncMethodBuilder(typeof(StreamTaskMethodBuilder))]
  public partial class StreamTask : ITask {

    public static StreamTask CompletedTask { get; } = new() { IsCompleted = true };
    public bool IsCompleted { get; private set; }

    [CanBeNull] internal Exception Error { get; private set; }

    private readonly Queue<(StreamTask nextTask, Action continuation)> _continuations = new(5);
    private readonly Queue<(StreamTask nextTask, Func<StreamTask> asyncContinuation)> _asyncContinuations = new(5);

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

      var nextTask = new StreamTask();
      _continuations.Enqueue((nextTask, continuation));
      return nextTask;
    }

    public StreamTask ContinueWith([NotNull] Func<StreamTask> continuation) {
      if (continuation == null)
        throw new ArgumentNullException(nameof(continuation));

      var nextTask = new StreamTask();

      if (IsCompleted)
        continuation().ContinueWith(nextTask.SetResult);
      else
        _asyncContinuations.Enqueue((nextTask, continuation));

      return nextTask;
    }

    public StreamTask WithCancellation(StreamToken token) {
      token.Register(this);
      return this;
    }

    internal void SetResult() {
      Complete();
    }

    internal void SetCanceled() {
      ((ITask)this).SetCanceled();
    }

    void ITask.SetCanceled() {
      Complete(new OperationCanceledException());
    }

    internal void SetException([NotNull] Exception exception) {
      if (exception == null)
        throw new ArgumentNullException(nameof(exception));
      Complete(exception);
    }

    private void Complete(Exception error = null) {
      if (IsCompleted)
        return;

      Error = error;

      while (_continuations.TryDequeue(out (StreamTask nextTask, Action continuation) x)) {
        x.continuation();
        if (error == null)
          x.nextTask.SetResult();
        else
          x.nextTask.SetException(error);
      }

      while (_asyncContinuations.TryDequeue(out (StreamTask nextTask, Func<StreamTask> asyncContinuation) x)) {
        if (error == null) {
          x.asyncContinuation().ContinueWith(x.nextTask.SetResult);
        }
        else {
          StreamTask nextTask = x.nextTask;
          x.asyncContinuation().ContinueWith(() => nextTask.SetException(error));
        }
      }

      IsCompleted = true;
    }

  }

  [AsyncMethodBuilder(typeof(StreamTaskMethodBuilder<>))]
  public class StreamTask<TResult> : ITask {

    public bool IsCompleted { get; private set; }
    [CanBeNull] internal Exception Error { get; private set; }
    internal TResult Result { get; private set; }

    private readonly Queue<(StreamTask<TResult> nextTask, Action<TResult> continuation)> _continuations = new(5);
    private readonly Queue<(StreamTask<TResult> nextTask, Func<TResult, StreamTask<TResult>> asyncContinuation)> _asyncContinuations = new(5);

    internal StreamTask() {
    }

    public StreamTaskAwaiter<TResult> GetAwaiter() {
      return new StreamTaskAwaiter<TResult>(this);
    }

    public StreamTask<TResult> ContinueWith([NotNull] Action<TResult> continuation) {
      if (continuation == null)
        throw new ArgumentNullException(nameof(continuation));

      if (IsCompleted) {
        continuation(Result);
        return this;
      }

      var nextTask = new StreamTask<TResult>();
      _continuations.Enqueue((nextTask, continuation));
      return nextTask;
    }

    public StreamTask<TResult> ContinueWith([NotNull] Func<TResult, StreamTask<TResult>> continuation) {
      if (continuation == null)
        throw new ArgumentNullException(nameof(continuation));

      var nextTask = new StreamTask<TResult>();

      if (IsCompleted)
        continuation(Result).ContinueWith(nextTask.SetResult);
      else
        _asyncContinuations.Enqueue((nextTask, continuation));

      return nextTask;
    }

    public StreamTask<TResult> WithCancellation(StreamToken token) {
      token.Register(this);
      return this;
    }

    internal void SetResult(TResult result) {
      Complete(result);
    }

    void ITask.SetCanceled() {
      Complete(default, new OperationCanceledException());
    }

    internal void SetException([NotNull] Exception exception) {
      if (exception == null)
        throw new ArgumentNullException(nameof(exception));
      Complete(default, exception);
    }

    private void Complete(TResult result, Exception error = null) {
      if (IsCompleted)
        return;

      Result = result;
      Error = error;

      while (_continuations.TryDequeue(out (StreamTask<TResult> nextTask, Action<TResult> continuation) x)) {
        x.continuation(result);
        if (error == null)
          x.nextTask.SetResult(result);
        else
          x.nextTask.SetException(error);
      }

      while (_asyncContinuations.TryDequeue(out (StreamTask<TResult> nextTask, Func<TResult, StreamTask<TResult>> asyncContinuation) x)) {
        if (error == null) {
          x.asyncContinuation(result).ContinueWith(x.nextTask.SetResult);
        }
        else {
          StreamTask<TResult> nextTask = x.nextTask;
          x.asyncContinuation(result).ContinueWith(_ => nextTask.SetException(error));
        }
      }

      IsCompleted = true;
    }

  }

}