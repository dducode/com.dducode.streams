using System;
using JetBrains.Annotations;

namespace Streams.StreamTasks.TaskSources {

  internal class StreamTaskSource : IStreamTaskSource {

    public StreamTask Task => new(this, _version);
    private protected StreamToken CancellationToken { get; private set; } = StreamToken.None;

    private StreamTaskStatus _status;
    private Action _continuation;
    private Exception _error;
    private short _version;

    public void GetResult(short version) {
      if (_version != version)
        throw new InvalidOperationException("Cannot use stream task when its source is reset");

      try {
        if (_error != null)
          throw _error;
      }
      finally {
        Reset();
      }
    }

    public StreamTaskStatus GetStatus(short version) {
      if (_version != version)
        throw new InvalidOperationException("Cannot use stream task when its source is reset");
      return _status;
    }

    public void OnCompleted(Action continuation, short version) {
      if (_version != version)
        throw new InvalidOperationException("Cannot use stream task when its source is reset");
      if (_continuation != null)
        throw new InvalidOperationException("Cannot use continuation after the previous continuation");
      _continuation = continuation;
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
    }

    internal void SetCanceled() {
      Complete(new OperationCanceledException());
    }

    internal void SetException([NotNull] Exception error) {
      if (error == null)
        throw new ArgumentNullException(nameof(error));
      Complete(error);
    }

    private protected virtual void Reset() {
      CancellationToken = StreamToken.None;
      _status = StreamTaskStatus.Pending;
      _error = null;
      _continuation = null;

      unchecked {
        _version++;
      }

      Pool.Return(this);
    }

    private void Complete(Exception error = null) {
      if (_status != StreamTaskStatus.Pending)
        return;

      _error = error;
      if (_error == null)
        _status = StreamTaskStatus.Succeeded;
      else
        _status = _error is OperationCanceledException ? StreamTaskStatus.Canceled : StreamTaskStatus.Faulted;

      _continuation?.Invoke();
    }

  }

  internal class StreamTaskSource<TResult> : IStreamTaskSource<TResult> {

    public StreamTask<TResult> Task => new(this, _version);
    private protected StreamToken CancellationToken { get; private set; } = StreamToken.None;

    private TResult _result;
    private StreamTaskStatus _status;
    private Action _continuation;
    private Exception _error;
    private short _version;

    public TResult GetResult(short version) {
      if (_version != version)
        throw new InvalidOperationException("Cannot use stream task when its source is reset");

      try {
        if (_error != null)
          throw _error;
        return _result;
      }
      finally {
        Reset();
      }
    }

    public StreamTaskStatus GetStatus(short version) {
      if (_version != version)
        throw new InvalidOperationException("Cannot use stream task when its source is reset");
      return _status;
    }

    public void OnCompleted(Action continuation, short version) {
      if (_version != version)
        throw new InvalidOperationException("Cannot use stream task when its source is reset");
      if (_continuation != null)
        throw new InvalidOperationException("Cannot use continuation after the previous continuation");
      _continuation = continuation;
    }

    public void SetCancellation(StreamToken cancellationToken) {
      CancellationToken = cancellationToken;
    }

    internal void SetResult(TResult result) {
      if (CancellationToken.Released) {
        SetCanceled();
        return;
      }

      Complete(result);
    }

    internal void SetCanceled() {
      Complete(default, new OperationCanceledException());
    }

    internal void SetException([NotNull] Exception error) {
      if (error == null)
        throw new ArgumentNullException(nameof(error));
      Complete(default, error);
    }

    private protected virtual void Reset() {
      CancellationToken = StreamToken.None;
      _result = default;
      _status = StreamTaskStatus.Pending;
      _error = null;
      _continuation = null;

      unchecked {
        _version++;
      }

      Pool.Return(this);
    }

    private void Complete(TResult result, Exception error = null) {
      if (_status != StreamTaskStatus.Pending)
        return;

      _result = result;
      _error = error;
      if (_error == null)
        _status = StreamTaskStatus.Succeeded;
      else
        _status = _error is OperationCanceledException ? StreamTaskStatus.Canceled : StreamTaskStatus.Faulted;

      _continuation?.Invoke();
    }

  }

}