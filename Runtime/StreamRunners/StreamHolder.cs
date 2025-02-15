namespace StreamsForUnity.StreamRunners {

  public sealed class StreamHolder<TBaseSystem> : IStreamHolder {

    public ExecutionStream Stream {
      get {
        ValidateRunnerState();
        return _stream;
      }
      private set => _stream = value;
    }

    public uint Priority {
      get {
        ValidateRunnerState();
        return _priority;
      }
      private set => _priority = value;
    }

    private ExecutionStream _stream;
    private uint _priority;

    private StreamAction _execution;
    private StreamTokenSource _subscriptionHandle;
    private bool _disposed;

    public StreamHolder(StreamToken disposeToken, string name, uint priority = uint.MaxValue) {
      Stream = new ExecutionStream(disposeToken, name);
      _subscriptionHandle = new StreamTokenSource();
      disposeToken.Register(Dispose);
      _execution = Streams.Get<TBaseSystem>().Add(Stream.Update, _subscriptionHandle.Token, Priority);
      Priority = priority;
    }

    public void ChangePriority(uint priority) {
      ValidateRunnerState();
      _execution.ChangePriority(priority);
      Priority = priority;
    }

    public IStreamHolder Join(IStreamHolder other) {
      ValidateRunnerState();
      if (other.Priority < Priority)
        return other.Join(this);

      Stream.Join(other.Stream);
      other.Dispose();
      return this;
    }

    public void Dispose() {
      _subscriptionHandle.Release();
      _subscriptionHandle = null;
      _execution = null;
      Stream = null;
      _disposed = true;
    }

    private void ValidateRunnerState() {
      if (_disposed)
        throw new StreamsException("Stream runner has been disposed");
    }

  }

}