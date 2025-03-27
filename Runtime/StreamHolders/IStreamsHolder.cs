using System;

namespace Streams.StreamHolders {

  /// <summary>
  /// Base contract fol all stream holders
  /// </summary>
  public interface IStreamsHolder {

    public ExecutionStream GetStream<TSystem>();
    public ExecutionStream GetStream(Type systemType);

  }

}