namespace StreamsForUnity.StreamRunners {

  public interface IStreamRunner {

    public ExecutionStream Stream { get; }
    uint Priority { get; }
    public void ChangePriority(uint priority);
    public IStreamRunner Join(IStreamRunner other);
    public void Dispose();

  }

}