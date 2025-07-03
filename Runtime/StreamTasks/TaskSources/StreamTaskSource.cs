using System;
using System.Collections.Generic;
using JetBrains.Annotations;

namespace Streams.StreamTasks.TaskSources {

  public class StreamTaskSource : IStreamTaskSource {

    public StreamTask Task => new(this, _version);
    internal static StreamTaskSource CompletedSource { get; } = new() { Status = StreamTaskStatus.Succeeded };
    private protected StreamTaskStatus Status { get; private set; }
    private protected StreamToken CancellationToken { get; private set; } = StreamToken.None;

    private readonly Queue<Action> _continuations = new();
    private Exception _error;
    private short _version;

    internal StreamTaskSource() {
    }

    public void GetResult(short version) {
      if (_version != version)
        throw new InvalidOperationException("Cannot use stream task when its source is reset");
      if (_error != null)
        throw _error;
    }

    public StreamTaskStatus GetStatus(short version) {
      if (_version != version)
        throw new InvalidOperationException("Cannot use stream task when its source is reset");
      return Status;
    }

    public void OnCompleted(Action continuation, short version) {
      if (_version != version)
        throw new InvalidOperationException("Cannot use stream task when its source is reset");
      _continuations.Enqueue(continuation);
    }

    public virtual void Reset() {
      Status = StreamTaskStatus.Pending;
      _error = null;
      CancellationToken = StreamToken.None;

      unchecked {
        _version++;
      }
    }

    public void SetCancellation(StreamToken cancellationToken) {
      CancellationToken = cancellationToken;
    }

    internal void SetResult() {
      if (CancellationToken.Released) {
        SetCanceled();
        return;
      }

      Complete();
      Status = StreamTaskStatus.Succeeded;
    }

    internal void SetCanceled() {
      Complete(new OperationCanceledException());
      Status = StreamTaskStatus.Canceled;
    }

    internal void SetException([NotNull] Exception error) {
      if (error == null)
        throw new ArgumentNullException(nameof(error));
      Complete(error);
      Status = StreamTaskStatus.Faulted;
    }

    private void Complete(Exception error = null) {
      if (Status != StreamTaskStatus.Pending)
        return;

      _error = error;
      while (_continuations.TryDequeue(out Action continuation)) 
        continuation();
      TaskSourcePool.Return(this);
    }

  }

  public class StreamTaskSource<TResult> : IStreamTaskSource<TResult> {

    public StreamTask<TResult> Task => new(this, _version);
    StreamTask IStreamTaskSource.Task => new(this, _version);
    private protected StreamTaskStatus Status { get; private set; }
    private protected StreamToken CancellationToken { get; private set; } = StreamToken.None;

    private readonly Queue<Action> _continuations = new();
    private TResult _result;
    private Exception _error;
    private short _version;

    public TResult GetResult(short version) {
      if (_version != version)
        throw new InvalidOperationException("Cannot use stream task when its source is reset");
      if (_error != null)
        throw _error;
      return _result;
    }

    public StreamTaskStatus GetStatus(short version) {
      if (_version != version)
        throw new InvalidOperationException("Cannot use stream task when its source is reset");
      return Status;
    }

    public void OnCompleted(Action continuation, short version) {
      if (_version != version)
        throw new InvalidOperationException("Cannot use stream task when its source is reset");
      _continuations.Enqueue(continuation);
    }

    public virtual void Reset() {
      _result = default;
      Status = StreamTaskStatus.Pending;
      _error = null;
      CancellationToken = StreamToken.None;

      unchecked {
        _version++;
      }
    }

    public void SetCancellation(StreamToken cancellationToken) {
      CancellationToken = cancellationToken;
    }

    void IStreamTaskSource.GetResult(short version) {
      GetResult(version);
    }

    internal void SetResult(TResult result) {
      if (CancellationToken.Released) {
        SetCanceled();
        return;
      }

      Complete(result);
      Status = StreamTaskStatus.Succeeded;
    }

    internal void SetCanceled() {
      Complete(default, new OperationCanceledException());
      Status = StreamTaskStatus.Canceled;
    }

    internal void SetException([NotNull] Exception error) {
      if (error == null)
        throw new ArgumentNullException(nameof(error));
      Complete(default, error);
      Status = StreamTaskStatus.Faulted;
    }

    private void Complete(TResult result, Exception error = null) {
      if (Status != StreamTaskStatus.Pending)
        return;

      _result = result;
      _error = error;
      while (_continuations.TryDequeue(out Action continuation))
        continuation();
      TaskSourcePool.Return(this);
    }

  }

}