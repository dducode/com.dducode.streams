namespace StreamsForUnity.StreamHolders {

  public interface IStreamHolder {

    public ManagedExecutionStream Stream { get; }
    public IStreamHolder Join(IStreamHolder other);
    public void Dispose();

  }

}