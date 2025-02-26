using System;
using UnityEngine;

namespace StreamsForUnity {

  public sealed class ManagedExecutionStream : ExecutionStream, IDisposable {

    public bool Locked => _lockers > 0;
    public StreamUnlockMode UnlockMode { get; }

    public uint Priority {
      get {
        ValidateStreamState();
        return _priority;
      }
      set {
        ValidateStreamState();
        if (_priority == value)
          return;

        _execution.ChangePriority(_priority = value);
      }
    }

    public float Delta {
      get {
        ValidateStreamState();
        return _delta ?? throw new ArgumentNullException(nameof(Delta));
      }
      set {
        ValidateStreamState();
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
        ValidateStreamState();
        if (_tickRate == 0)
          throw new ArgumentOutOfRangeException(nameof(TickRate), "Tick rate cannot be zero");
        if (_tickRate == value)
          return;

        _execution.SetTickRate(_tickRate = value);
      }
    }

    private uint _priority;
    private float? _delta;
    private uint _tickRate = 1;
    private StreamTokenSource _subscriptionHandle;
    private StreamAction _execution;
    private int _lockers;

    public ManagedExecutionStream(
      ExecutionStream baseStream,
      string name = nameof(ManagedExecutionStream),
      uint priority = uint.MaxValue,
      StreamUnlockMode unlockMode = StreamUnlockMode.WhenAll
    ) : base(name) {
      _subscriptionHandle = new StreamTokenSource();
      _execution = baseStream.Add(Update, _subscriptionHandle.Token, _priority = priority);
      UnlockMode = unlockMode;
    }

    public void Lock(StreamToken lockToken) {
      switch (UnlockMode) {
        case StreamUnlockMode.WhenAll:
          _lockers++;
          lockToken.Register(() => _lockers--);
          break;
        case StreamUnlockMode.WhenAny:
          _lockers = 1;
          lockToken.Register(() => _lockers = 0);
          break;
        default:
          throw new ArgumentOutOfRangeException();
      }
    }

    public void Join(ManagedExecutionStream other) {
      ExecutionStream runningStream = Streams.RunningStream;
      if (runningStream == this || runningStream == other)
        throw new StreamsException($"Cannot join a running stream ({runningStream})");

      actionsStorage.Join(other.actionsStorage);
      parallelActionsStorage.Join(other.parallelActionsStorage);
      delayedCallbacks += other.delayedCallbacks;
      disposeCallbacks += other.disposeCallbacks;
      other.SilentDispose();
    }

    public void Reconnect(ExecutionStream stream, uint? priority = null) {
      _subscriptionHandle?.Release();
      _subscriptionHandle = new StreamTokenSource();
      _execution = stream.Add(Update, _subscriptionHandle.Token, _priority = priority ?? _priority).SetTickRate(_tickRate);

      if (_delta.HasValue)
        _execution.SetDelta(_delta.Value);
    }

    public void ResetDelta() {
      _delta = null;
      _execution.ResetDelta();
    }

    public void Dispose() {
      StreamState = StreamState.Disposing;
      _subscriptionHandle.Release();
      _subscriptionHandle = null;
      _execution = null;
      Dispose_Internal();
      StreamState = StreamState.Disposed;
    }

    protected override bool CanExecute() {
      return base.CanExecute() && !Locked;
    }

    private void ValidateStreamState() {
      if (StreamState is StreamState.Disposing or StreamState.Disposed)
        throw new StreamsException("Stream is disposed");
    }

    private void SilentDispose() {
      disposeCallbacks = null;
      delayedCallbacks = null;
      StreamState = StreamState.Disposed;
    }

  }

}