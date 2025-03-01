using System;
using StreamsForUnity.Exceptions;
using UnityEngine;

namespace StreamsForUnity {

  /// <summary>
  /// <p> The custom stream that you can control. You can <see cref="Lock">lock</see> this stream, <see cref="Join">join</see> another
  /// stream with it, and <see cref="Reconnect">reconnect</see> to another stream. </p>
  /// <p> You can also configure the <see cref="Priority"/> of the stream, <see cref="Delta"/> and <see cref="TickRate"/> of the stream execution </p>
  /// </summary>
  public sealed class ManagedExecutionStream : ExecutionStream, IDisposable {

    public bool Locked => _lockers > 0;

    /// <inheritdoc cref="StreamUnlockMode"/>
    /// <remarks> The unlock behavior is set when the stream is created and doesn't change during the lifetime of the stream </remarks>
    public StreamUnlockMode UnlockMode { get; }

    /// <summary>
    /// Gets and sets the priority of the stream relative to other streams of the parent stream
    /// </summary>
    /// <exception cref="StreamDisposedException"> Threw if the stream is disposed </exception>
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

    /// <summary>
    /// Gets and sets the stream execution delta
    /// </summary>
    /// <exception cref="ArgumentNullException"> Threw on get if delta isn't yet installed </exception>
    /// <exception cref="ArgumentOutOfRangeException"> Threw on set if the passed value is negative or zero </exception>
    /// <exception cref="StreamDisposedException"> Threw if the stream is disposed </exception>
    public float Delta {
      get {
        ValidateStreamState();
        return _delta ?? throw new ArgumentNullException(nameof(Delta));
      }
      set {
        ValidateStreamState();
        if (value <= 0f)
          throw new ArgumentOutOfRangeException(nameof(Delta), "Delta cannot be negative or zero");
        if (_delta.HasValue && Mathf.Approximately(_delta.Value, value))
          return;

        _execution.SetDelta((_delta = value).Value);
      }
    }

    /// <returns> True if the delta has been installed </returns>
    /// <exception cref="StreamDisposedException"> Threw if the stream is disposed </exception>
    public bool HasDelta {
      get {
        ValidateStreamState();
        return _delta.HasValue;
      }
    }

    /// <summary>
    /// Gets and sets the stream execution tick rate
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException"> Threw if the passed value is zero </exception>
    /// <exception cref="StreamDisposedException"> Threw if the stream is disposed </exception>
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

    /// <summary>
    /// Call this to lock the stream on a token
    /// </summary>
    /// <seealso cref="UnlockMode"/>
    /// <exception cref="StreamDisposedException"> Threw if the stream is disposed </exception>
    public void Lock(StreamToken lockToken) {
      ValidateStreamState();
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

    /// <summary>
    /// Join another stream with all its actions
    /// </summary>
    /// <param name="other"> The stream to be joined </param>
    /// <returns> The returned stream depends on the priority of both - if the current stream has higher priority,
    /// the other stream will be joined to the current one, otherwise vice versa </returns>
    /// <exception cref="StreamsException"> Threw when one of the streams is currently running </exception>
    /// <exception cref="StreamDisposedException"> Threw if the stream is disposed </exception>
    public ManagedExecutionStream Join(ManagedExecutionStream other) {
      ValidateStreamState();
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

    /// <summary>
    /// Reconnect the stream to another parent stream
    /// </summary>
    /// <param name="stream"> The stream in which the current one will be executed </param>
    /// <param name="priority"> <see cref="Priority"/> </param>
    /// <exception cref="StreamDisposedException"> Threw if the stream is disposed </exception>
    public void Reconnect(ExecutionStream stream, uint? priority = null) {
      ValidateStreamState();
      _subscriptionHandle?.Release();
      _subscriptionHandle = new StreamTokenSource();

      _baseStream.RemoveDisposeHandle(Dispose);
      _baseStream = stream;
      _execution = _baseStream.Add(Update, _subscriptionHandle.Token, _priority = priority ?? _priority).SetTickRate(_tickRate);
      _baseStream.OnDispose(Dispose);

      if (_delta.HasValue)
        _execution.SetDelta(_delta.Value);
    }

    /// <summary>
    /// Reset the stream execution delta
    /// </summary>
    /// <exception cref="StreamDisposedException"> Threw if the stream is disposed </exception>
    public void ResetDelta() {
      ValidateStreamState();
      _delta = null;
      _execution.ResetDelta();
    }

    public void Dispose() {
      if (State is StreamState.Terminating or StreamState.Terminated)
        return;

      _subscriptionHandle.Release();
      _subscriptionHandle = null;
      _execution = null;
      Terminate();
    }

    protected override bool CanExecute() {
      return base.CanExecute() && !Locked;
    }

    private void ValidateStreamState() {
      if (State is StreamState.Terminating or StreamState.Terminated)
        throw new StreamDisposedException(ToString());
    }

  }

}