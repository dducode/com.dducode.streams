using System;
using UnityEngine;

namespace StreamsForUnity.StreamHolders {

  public sealed class StreamHolder<TBaseSystem> : IConfiguredStreamHolder {

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

    public float Delta {
      get {
        ValidateHolderState();
        return _delta ?? throw new ArgumentNullException(nameof(Delta));
      }
      set {
        ValidateHolderState();
        if (value < 0f)
          throw new ArgumentOutOfRangeException(nameof(Delta), "Delta cannot be negative");
        if (_delta.HasValue && Mathf.Approximately(_delta.Value, value))
          return;

        _execution.SetDelta((_delta = value).Value);
      }
    }

    public uint TickRate {
      get => _tickRate;
      set {
        ValidateHolderState();
        if (_tickRate == 0)
          throw new ArgumentOutOfRangeException(nameof(TickRate), "Tick rate cannot be zero");
        if (_tickRate == value)
          return;

        _execution.SetTickRate(_tickRate = value);
      }
    }

    private ExecutionStream _stream;
    private uint _priority;
    private float? _delta;

    private StreamAction _execution;
    private StreamTokenSource _subscriptionHandle;
    private bool _disposed;
    private uint _tickRate;

    public StreamHolder(StreamToken disposeToken, string name, uint priority = uint.MaxValue) {
      Stream = new ExecutionStream(disposeToken, name);
      _subscriptionHandle = new StreamTokenSource();
      disposeToken.Register(Dispose);
      _execution = Streams.Get<TBaseSystem>().Add(Stream.Update, _subscriptionHandle.Token);
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

    public void ResetDelta() {
      _delta = null;
      _execution.ResetDelta();
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