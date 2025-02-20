namespace StreamsForUnity.StreamHolders {

  public interface IStreamHolder {

    public ExecutionStream Stream { get; }
    public uint Priority { get; set; }
    public float Delta { get; set; }
    public IStreamHolder Join(IStreamHolder other);
    public void Dispose();

  }

}