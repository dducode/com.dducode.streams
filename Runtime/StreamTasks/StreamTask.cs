using System.Runtime.CompilerServices;
using Streams.StreamTasks.TaskSources;

namespace Streams.StreamTasks {

  [AsyncMethodBuilder(typeof(StreamTaskMethodBuilder))]
  public readonly partial struct StreamTask {

    public static StreamTask CompletedTask { get; } = new(StreamTaskSource.CompletedSource, 0);

    private readonly IStreamTaskSource _source;
    private readonly short _version;

    public StreamTask(IStreamTaskSource source, short version) {
      _source = source;
      _version = version;
    }

    public StreamTaskAwaiter GetAwaiter() {
      return new StreamTaskAwaiter(_source, _version);
    }

    public StreamTask WithCancellation(StreamToken cancellationToken) {
      _source.SetCancellation(cancellationToken);
      return this;
    }

  }

  public readonly struct StreamTask<TResult> {

    private readonly IStreamTaskSource<TResult> _source;
    private readonly short _version;

    public StreamTask(IStreamTaskSource<TResult> source, short version) {
      _source = source;
      _version = version;
    }

    public StreamTaskAwaiter<TResult> GetAwaiter() {
      return new StreamTaskAwaiter<TResult>(_source, _version);
    }

  }

}