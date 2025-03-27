namespace Streams {

  /// <summary>
  /// Defines the parallel work strategy that determines the intensity of parallel execution.
  /// </summary>
  public enum ParallelWorkStrategy {

    /// <summary>
    /// Minimizes resource consumption by using fewer threads.
    /// </summary>
    Economy,

    /// <summary>
    /// Balances performance and resource usage.
    /// </summary>
    Optimal,

    /// <summary>
    /// Maximizes parallelism by utilizing all available CPU resources.
    /// </summary>
    Performance

  }

}