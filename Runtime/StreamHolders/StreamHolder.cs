using UnityEngine;

namespace StreamsForUnity.StreamHolders {

  public sealed class StreamHolder<TBaseSystem> : IStreamHolder {

    public ExecutionStream Stream {
      get {
        ValidateHolderState();
        return _stream;
      }
      private set => _stream = value;
    }

    public bool IsParallel {
      get {
        ValidateHolderState();
        return _isParallel;
      }
      set {
        ValidateHolderState();
        if (_isParallel == value)
          return;

        _stream.IsParallel = _isParallel = value;
        ReconnectStream();
      }
    }

    public uint Priority {
      get {
        ValidateHolderState();
        return _priority;
      }
      set {
        ValidateHolderState();
        if (_isParallel) {
          Debug.LogWarning("Priority has no effect if the stream is registered for parallel execution");
          _priority = value;
          return;
        }

        if (_priority == value)
          return;

        _execution.ChangePriority(_priority = value);
      }
    }

    private ExecutionStream _stream;
    private uint _priority;
    private bool _isParallel;

    private StreamAction _execution;
    private StreamTokenSource _subscriptionHandle;
    private bool _disposed;

    public StreamHolder(StreamToken disposeToken, string name, uint priority = uint.MaxValue, bool isParallel = false) {
      Stream = new ExecutionStream(disposeToken, name);
      _subscriptionHandle = new StreamTokenSource();
      disposeToken.Register(Dispose);
      _execution = isParallel
        ? Streams.Get<TBaseSystem>().AddParallel(Stream.Update, _subscriptionHandle.Token)
        : Streams.Get<TBaseSystem>().Add(Stream.Update, _subscriptionHandle.Token, priority);

      _priority = priority;
      _isParallel = isParallel;
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

    private void ReconnectStream() {
      _subscriptionHandle.Release();
      _subscriptionHandle = new StreamTokenSource();
      _execution = _isParallel
        ? Streams.Get<TBaseSystem>().AddParallel(Stream.Update, _subscriptionHandle.Token)
        : Streams.Get<TBaseSystem>().Add(Stream.Update, _subscriptionHandle.Token, _priority);
    }

    private void ValidateHolderState() {
      if (_disposed)
        throw new StreamsException("Stream runner has been disposed");
    }

  }

}