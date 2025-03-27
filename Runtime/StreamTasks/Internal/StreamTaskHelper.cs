using StreamsForUnity.Exceptions;

namespace StreamsForUnity.StreamTasks.Internal {

  internal static class StreamTaskHelper {

    public static ExecutionStream GetRunningStream() {
      return ExecutionStream.RunningStream ?? throw new StreamsException("Cannot use stream tasks outside of stream execution");
    }

  }

}