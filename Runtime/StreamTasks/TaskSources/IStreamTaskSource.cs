using System;

namespace Streams.StreamTasks.TaskSources {

  public interface IStreamTaskSource {

    StreamTask Task { get; }
    void GetResult(short version);
    StreamTaskStatus GetStatus(short version);
    void OnCompleted(Action continuation, short version);
    void Reset();
    void SetCancellation(StreamToken cancellationToken);

  }

  public interface IStreamTaskSource<TResult> : IStreamTaskSource {

    new StreamTask<TResult> Task { get; }
    new TResult GetResult(short version);

  }

}