using System.Threading;

namespace StreamsForUnity.StreamRunners {

  public sealed class StreamRunner<TBaseSystem> : IStreamRunner {

    public ExecutionStream Stream { get; }

    public StreamRunner(CancellationToken disposeToken, string name) {
      Stream = new ExecutionStream(disposeToken, name);
      Streams.Get<TBaseSystem>().Add(Stream.Update, disposeToken);
    }

  }

}