using System;
using System.Runtime.CompilerServices;
using Streams.StreamTasks.TaskSources;

namespace Streams.StreamTasks {

  public readonly struct StreamTaskAwaiter : ICriticalNotifyCompletion {

    public bool IsCompleted {
      get {
        if (_source == null)
          return true;
        return _source.GetStatus(_version) != StreamTaskStatus.Pending;
      }
    }

    private readonly IStreamTaskSource _source;
    private readonly short _version;

    internal StreamTaskAwaiter(IStreamTaskSource source, short version) {
      _source = source;
      _version = version;
    }

    public void OnCompleted(Action continuation) {
      _source.OnCompleted(continuation, _version);
    }

    public void UnsafeOnCompleted(Action continuation) {
      OnCompleted(continuation);
    }

    public void GetResult() {
      _source?.GetResult(_version);
    }

  }

  public readonly struct StreamTaskAwaiter<TResult> : ICriticalNotifyCompletion {

    public bool IsCompleted => _source.GetStatus(_version) != StreamTaskStatus.Pending;

    private readonly IStreamTaskSource<TResult> _source;
    private readonly short _version;

    internal StreamTaskAwaiter(IStreamTaskSource<TResult> source, short version) {
      _source = source;
      _version = version;
    }

    public void OnCompleted(Action continuation) {
      _source.OnCompleted(continuation, _version);
    }

    public void UnsafeOnCompleted(Action continuation) {
      OnCompleted(continuation);
    }

    public TResult GetResult() {
      return _source.GetResult(_version);
    }

  }

}