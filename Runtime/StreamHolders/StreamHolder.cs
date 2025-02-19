namespace StreamsForUnity.StreamHolders {

  public sealed class StreamHolder<TBaseSystem> : IStreamHolder {

    public ExecutionStream Stream {
      get {
        ValidateHolderState();
        return _stream;
      }
      private set => _stream = value;
    }

    public uint Priority {
      get {
        ValidateHolderState();
        return _priority;
      }
      set {
        ValidateHolderState();
        if (_priority == value)
          return;

        _execution.ChangePriority(_priority = value);
      }
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
      _execution = Streams.Get<TBaseSystem>().Add(Stream.Update, _subscriptionHandle.Token, priority);
      _priority = priority;
    }

    public IStreamHolder Join(IStreamHolder other) {
      ValidateHolderState();
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
      _stream = null;
      _disposed = true;
    }

    private void ValidateHolderState() {
      if (_disposed)
        throw new StreamsException("Stream runner has been disposed");
    }

  }

}