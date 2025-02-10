using System;
using System.Threading;

namespace StreamsForUnity.StreamTasks {

  public partial class StreamTask {

    public static StreamTask Yield(CancellationToken token = default) {
      var task = new StreamTask();
      GetRunningStream().AddOnce(task.SetResult, token);
      token.Register(task.SetCanceled);
      return task;
    }

    public static StreamTask Delay(uint milliseconds, CancellationToken token = default) {
      var task = new StreamTask();
      GetRunningStream().AddTimer(milliseconds / 1000f, task.SetResult, token);
      token.Register(task.SetCanceled);
      return task;
    }

    public static StreamTask WaitWhile(Func<bool> condition, CancellationToken token = default) {
      var task = new StreamTask();
      var cts = new CancellationTokenSource();
      GetRunningStream().Add(_ => {
        if (condition())
          return;

        task.SetResult();
        cts.Cancel();
      }, cts.Token);

      token.Register(() => {
        task.SetCanceled();
        cts.Cancel();
      });
      return task;
    }

    private static ExecutionStream GetRunningStream() {
      return Streams.RunningStream ?? throw new StreamsException("Cannot use stream tasks outside of stream execution");
    }

  }

}