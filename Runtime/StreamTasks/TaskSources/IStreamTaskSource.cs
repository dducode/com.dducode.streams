using System;

namespace Streams.StreamTasks.TaskSources {

  internal interface IStreamTaskSource {

    StreamTask Task { get; }
    void GetResult(short version);
    StreamTaskStatus GetStatus(short version);
    void OnCompleted(Action continuation, short version);
    void SetCancellation(StreamToken cancellationToken);

  }

  internal interface IStreamTaskSource<TResult> : IStreamTaskSource {

    new StreamTask<TResult> Task { get; }
    new TResult GetResult(short version);

  }

}