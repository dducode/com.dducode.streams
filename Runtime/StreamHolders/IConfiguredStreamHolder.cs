namespace StreamsForUnity.StreamHolders {

  public interface IConfiguredStreamHolder : IStreamHolder {

    public float Delta { get; set; }
    public uint TickRate { get; set; }
    public void ResetDelta();

  }

}