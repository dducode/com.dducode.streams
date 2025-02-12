using System.Threading;

namespace StreamsForUnity.StreamRunners {

  public sealed class StreamRunner<TBaseSystem> : IStreamRunner {

    public ExecutionStream Stream { get; }
    private CancellationTokenSource _subscriptionHandle;

    public StreamRunner(CancellationToken disposeToken, string name, uint priority = uint.MaxValue) {
      Stream = new ExecutionStream(disposeToken, name);
      disposeToken.Register(Dispose);
      ConnectStream(priority);
    }

    public void ReconnectStream(uint priority) {
      _subscriptionHandle.Cancel();
      ConnectStream(priority);
    }

    private void ConnectStream(uint priority) {
      _subscriptionHandle = new CancellationTokenSource();
      Streams.Get<TBaseSystem>().Add(Stream.Update, _subscriptionHandle.Token, priority);
    }

    private void Dispose() {
      _subscriptionHandle.Cancel();
    }

  }

}