using System;
using Streams.StreamTasks;

namespace Streams.Extensions {

  public static class TimeSpanExtensions {

    public static StreamDelayAwaitable GetAwaiter(this TimeSpan timeSpan) {
      return StreamTask.WaitFor((int)timeSpan.TotalMilliseconds);
    }

  }

}