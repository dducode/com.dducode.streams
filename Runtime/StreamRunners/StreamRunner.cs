namespace StreamsForUnity.StreamRunners {

  public sealed class StreamRunner<TBaseSystem> : IStreamRunner {

    public ExecutionStream Stream { get; }
    private StreamTokenSource _subscriptionHandle;

    public StreamRunner(StreamToken disposeToken, string name, uint priority = uint.MaxValue) {
      Stream = new ExecutionStream(disposeToken, name);
      disposeToken.Register(Dispose);
      ConnectStream(priority);
    }

    public void ReconnectStream(uint priority) {
      _subscriptionHandle.Release();
      ConnectStream(priority);
    }

    private void ConnectStream(uint priority) {
      _subscriptionHandle = new StreamTokenSource();
      Streams.Get<TBaseSystem>().Add(Stream.Update, _subscriptionHandle.Token, priority);
    }

    private void Dispose() {
      _subscriptionHandle.Release();
    }

  }

}