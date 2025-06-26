using System.Collections.Generic;

namespace Streams.StreamContexts {

  internal static class ExecutionContexts {

    internal static Dictionary<ExecutionStream, IStreamExecutionContext> All { get; } = new();

  }

}