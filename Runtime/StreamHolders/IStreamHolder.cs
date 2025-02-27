namespace StreamsForUnity.StreamHolders {

  public interface IStreamHolder {

    public ManagedExecutionStream Stream { get; }
    public ManagedExecutionStream Join(ManagedExecutionStream other);

  }

}