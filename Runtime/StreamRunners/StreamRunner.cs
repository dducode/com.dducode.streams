namespace StreamsForUnity.StreamRunners {

  public sealed class StreamRunner<TBaseSystem> : IStreamRunner {

    public ExecutionStream Stream { get; }

    private readonly StreamAction _execution;

    public StreamRunner(StreamToken disposeToken, string name, uint priority = uint.MaxValue) {
      Stream = new ExecutionStream(disposeToken, name);
      _execution = Streams.Get<TBaseSystem>().Add(Stream.Update, disposeToken, priority);
    }

    public void ChangePriority(uint priority) {
      _execution.ChangePriority(priority);
    }

  }

}