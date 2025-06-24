using System;

namespace Streams.StreamContexts {

  /// <summary>
  /// Base contract fol all stream contexts
  /// </summary>
  public interface IStreamExecutionContext {

    /// <summary>
    /// Gets the stream from context in the specified player loop system
    /// </summary>
    /// <typeparam name="TSystem"> The system that contains the stream </typeparam>
    /// <returns> Existing stream or new </returns>
    public ExecutionStream GetStream<TSystem>();

    /// <summary>
    /// Gets the stream from context in the specified player loop system
    /// </summary>
    /// <param name="systemType"> The system that contains the stream </param>
    /// <returns> Existing stream or new </returns>
    public ExecutionStream GetStream(Type systemType);

  }

}