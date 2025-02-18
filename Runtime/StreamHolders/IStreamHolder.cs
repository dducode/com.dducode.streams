namespace StreamsForUnity.StreamHolders {

  public interface IStreamHolder {

    public ExecutionStream Stream { get; }
    public bool IsParallel { get; set; }
    public uint Priority { get; set; }
    public IStreamHolder Join(IStreamHolder other);
    public void Dispose();

  }

}