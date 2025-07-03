using System;
using Streams.StreamTasks;

namespace Streams.Extensions {

  public static class StreamTaskExtensions {

    public static async StreamTask ContinueWith(this StreamTask task, Action continuation) {
      await task;
      continuation();
    }

    public static async StreamTask ContinueWith(this StreamTask task, Func<StreamTask> continuation) {
      await task;
      await continuation();
    }

  }

}