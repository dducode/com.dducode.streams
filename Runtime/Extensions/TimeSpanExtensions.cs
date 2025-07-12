using System;
using Streams.StreamTasks;

namespace Streams.Extensions {

  public static class TimeSpanExtensions {

    public static StreamTask.Awaiter GetAwaiter(this TimeSpan timeSpan) {
      return StreamTask.Delay((int)timeSpan.TotalMilliseconds).GetAwaiter();
    }

  }

}