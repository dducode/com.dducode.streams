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
      get {
        ValidateStreamState();
        return _tickRate;
      }
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
    private ExecutionStream _baseStream;
    private StreamAction _execution;
    private int _lockers;

    public ManagedExecutionStream(
      ExecutionStream baseStream,
      string name = nameof(ManagedExecutionStream),
      uint priority = uint.MaxValue,
      StreamUnlockMode unlockMode = StreamUnlockMode.WhenAll
    ) : base(name) {
      _subscriptionHandle = new StreamTokenSource();
      _baseStream = baseStream;
      _execution = _baseStream.Add(Update, _subscriptionHandle.Token, _priority = priority);
      _baseStream.OnDispose(Dispose);
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

    public ManagedExecutionStream Join(ManagedExecutionStream other) {
      if (other.Priority < Priority)
        return other.Join(this);

      ExecutionStream runningStream = Streams.RunningStream;
      if (runningStream == this || runningStream == other)
        throw new StreamsException($"Cannot join a running stream ({runningStream})");

      actionsStorage.Join(other.actionsStorage);
      parallelActionsStorage.Join(other.parallelActionsStorage);
      delayedCallbacks += other.delayedCallbacks;
      disposeCallbacks += other.disposeCallbacks;
      other.Dispose();
      return this;
    }

    public void Reconnect(ExecutionStream stream, uint? priority = null) {
      _subscriptionHandle?.Release();
      _subscriptionHandle = new StreamTokenSource();

      _baseStream.RemoveDisposeHandle(Dispose);
      _baseStream = stream;
      _execution = _baseStream.Add(Update, _subscriptionHandle.Token, _priority = priority ?? _priority).SetTickRate(_tickRate);
      _baseStream.OnDispose(Dispose);

      if (_delta.HasValue)
        _execution.SetDelta(_delta.Value);
    }

    public void ResetDelta() {
      _delta = null;
      _execution.ResetDelta();
    }

    public void Dispose() {
      if (State is StreamState.Disposing or StreamState.Disposed)
        return;

      _subscriptionHandle.Release();
      _subscriptionHandle = null;
      _execution = null;
      Dispose_Internal();
    }

    protected override bool CanExecute() {
      return base.CanExecute() && !Locked;
    }

    private void ValidateStreamState() {
      if (State is StreamState.Disposing or StreamState.Disposed)
        throw new ObjectDisposedException(ToString());
    }

  }

}