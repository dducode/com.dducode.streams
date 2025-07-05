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

    public StreamTaskAwaiter GetAwaiter() {
      return new StreamTaskAwaiter(_source, _version);
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

  }

  [AsyncMethodBuilder(typeof(StreamTaskMethodBuilder<>))]
  public readonly struct StreamTask<TResult> : IEquatable<StreamTask<TResult>> {

    private readonly IStreamTaskSource<TResult> _source;
    private readonly short _version;

    internal StreamTask(IStreamTaskSource<TResult> source, short version) {
      _source = source;
      _version = version;
    }

    public StreamTaskAwaiter<TResult> GetAwaiter() {
      return new StreamTaskAwaiter<TResult>(_source, _version);
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

  }

}