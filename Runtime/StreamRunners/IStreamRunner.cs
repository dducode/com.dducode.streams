namespace StreamsForUnity.StreamRunners {

  public interface IStreamRunner {

    public ExecutionStream Stream { get; }
    public void ChangePriority(uint priority);

  }

}