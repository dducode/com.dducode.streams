using System;
using System.Runtime.CompilerServices;
using Streams.StreamTasks.TaskSources;

namespace Streams.StreamTasks {

  [AsyncMethodBuilder(typeof(StreamTaskMethodBuilder))]
  public readonly partial struct StreamTask : IEquatable<StreamTask> {

    public static StreamTask CompletedTask { get; } = new();

    private readonly IStreamTaskSource _source;
    private readonly short _version;

    internal StreamTask(IStreamTaskSource source, short version) {
      _source = source;
      _version = version;
    }

    public Awaiter GetAwaiter() {
      return new Awaiter(_source, _version);
    }

    public bool Equals(StreamTask other) {
      return Equals(_source, other._source) && _version == other._version;
    }

    public override bool Equals(object obj) {
      return obj is StreamTask other && Equals(other);
    }

    public override int GetHashCode() {
      return HashCode.Combine(_source, _version);
    }

    public readonly struct Awaiter : ICriticalNotifyCompletion {

      public bool IsCompleted {
        get {
          if (_source == null)
            return true;
          return _source.GetStatus(_version) != StreamTaskStatus.Pending;
        }
      }

      private readonly IStreamTaskSource _source;
      private readonly short _version;

      internal Awaiter(IStreamTaskSource source, short version) {
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

  }

  [AsyncMethodBuilder(typeof(StreamTaskMethodBuilder<>))]
  public readonly struct StreamTask<TResult> : IEquatable<StreamTask<TResult>> {

    private readonly IStreamTaskSource<TResult> _source;
    private readonly short _version;

    internal StreamTask(IStreamTaskSource<TResult> source, short version) {
      _source = source;
      _version = version;
    }

    public Awaiter GetAwaiter() {
      return new Awaiter(_source, _version);
    }

    public bool Equals(StreamTask<TResult> other) {
      return Equals(_source, other._source) && _version == other._version;
    }

    public override bool Equals(object obj) {
      return obj is StreamTask<TResult> other && Equals(other);
    }

    public override int GetHashCode() {
      return HashCode.Combine(_source, _version);
    }

    public readonly struct Awaiter : ICriticalNotifyCompletion {

      public bool IsCompleted => _source.GetStatus(_version) != StreamTaskStatus.Pending;

      private readonly IStreamTaskSource<TResult> _source;
      private readonly short _version;

      internal Awaiter(IStreamTaskSource<TResult> source, short version) {
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

}