using System;

namespace Streams.StreamHolders {

  /// <summary>
  /// Base contract fol all stream holders
  /// </summary>
  public interface IStreamsHolder {

    /// <summary>
    /// Gets the stream from holder in the specified player loop system
    /// </summary>
    /// <typeparam name="TSystem"> The system that contains the stream </typeparam>
    /// <returns> Existing stream or new </returns>
    public ExecutionStream GetStream<TSystem>();

    /// <summary>
    /// Gets the stream from holder in the specified player loop system
    /// </summary>
    /// <param name="systemType"> The system that contains the stream </param>
    /// <returns> Existing stream or new </returns>
    public ExecutionStream GetStream(Type systemType);

  }

}