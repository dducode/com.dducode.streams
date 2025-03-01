namespace StreamsForUnity.StreamHolders {

  /// <summary>
  /// Base contract fol all stream holders
  /// </summary>
  public interface IStreamHolder {

    /// <summary>
    /// The stream that is contained in the stream holder
    /// </summary>
    public ExecutionStream Stream { get; }

  }

}