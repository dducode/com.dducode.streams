namespace StreamsForUnity.StreamRunners {

  public interface IStreamHolder {

    public ExecutionStream Stream { get; }
    uint Priority { get; }
    public void ChangePriority(uint priority);
    public IStreamHolder Join(IStreamHolder other);
    public void Dispose();

  }

}