namespace StreamsForUnity {

  public interface IManagedExecutionStream : IExecutionStream {

    public void Lock(StreamToken lockToken);
    public bool Locked { get; }

  }

}