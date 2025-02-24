namespace StreamsForUnity.StreamTasks.Internal {

  internal static class StreamTaskHelper {

    public static IExecutionStream GetRunningStream() {
      return Streams.RunningStream ?? throw new StreamsException("Cannot use stream tasks outside of stream execution");
    }

  }

}