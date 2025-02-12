namespace StreamsForUnity.StreamRunners {

  public interface IStreamRunner {

    public ExecutionStream Stream { get; }
    public void ReconnectStream(uint priority);

  }

}