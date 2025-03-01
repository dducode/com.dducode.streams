namespace StreamsForUnity {

  /// <summary>
  /// Defines the current state of the stream
  /// </summary>
  public enum StreamState {

    /// <summary>
    /// Stream does nothing
    /// </summary>
    Idle,
    
    /// <summary>
    /// Stream is currently running
    /// </summary>
    Running,
    
    /// <summary>
    /// Stream is currently in the terminate process
    /// </summary>
    Terminating,
    
    /// <summary>
    /// Stream is terminated
    /// </summary>
    Terminated,
    
    /// <summary>
    /// Stream is in the invalid state
    /// </summary>
    Invalid

  }

}