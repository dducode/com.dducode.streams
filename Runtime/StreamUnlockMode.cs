namespace StreamsForUnity {

  /// <summary>
  /// Defines how the stream will be unlocked
  /// </summary>
  public enum StreamUnlockMode {

    /// <summary>
    /// Unlock the stream when all passed tokens have been released
    /// </summary>
    WhenAll,
    
    /// <summary>
    /// Unlock the stream when at least one passed token have been released
    /// </summary>
    WhenAny

  }

}