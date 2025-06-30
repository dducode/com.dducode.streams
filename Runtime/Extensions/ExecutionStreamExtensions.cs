namespace Streams.Extensions {

  public static class ExecutionStreamExtensions {

    public static StreamToken CreateTokenTerminatedAfter(this ExecutionStream stream, float time) {
      var cts = new StreamTokenSource();
      stream.AddDelayed(time, cts.Release);
      return cts.Token;
    }

  }

}