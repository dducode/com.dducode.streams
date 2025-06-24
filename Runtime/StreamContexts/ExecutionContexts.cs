using System.Collections.Generic;
using JetBrains.Annotations;

namespace Streams.StreamContexts {

  public static class ExecutionContexts {

    [CanBeNull] public static IStreamExecutionContext Current => All.GetValueOrDefault(ExecutionStream.RunningStream);
    internal static Dictionary<ExecutionStream, IStreamExecutionContext> All { get; } = new();

  }

}