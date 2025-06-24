using System;
using System.Threading;
using Streams.Exceptions;
using Streams.StreamActions;
using UnityEngine;

namespace Streams {

  /// <summary>
  /// <p> The custom stream that you can control. You can <see cref="Join">join</see> another stream with it and <see cref="Reconnect">reconnect</see> to another stream. </p>
  /// <p> You can also configure the <see cref="Priority"/> of the stream, <see cref="Delta"/> and <see cref="TickRate"/> of the stream execution </p>
  /// </summary>
  public sealed class ManagedExecutionStream : ExecutionStream, IDisposable, IJoinable<ManagedExecutionStream> {

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

        _execution.Priority = _priority = value;
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

        _execution.SetTickRate(_tickRate = 1);
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

        _delta = null;
        _execution.ResetDelta();
        _execution.SetTickRate(_tickRate = value);
      }
    }

    private uint _priority = uint.MaxValue;
    private float? _delta;
    private uint _tickRate = 1;
    private CancellationTokenSource _subscriptionHandle;
    private ExecutionStream _baseStream;
    private PersistentAction _execution;

    public ManagedExecutionStream(
      ExecutionStream baseStream,
      string name = nameof(ManagedExecutionStream)
    ) : base(name) {
      _subscriptionHandle = new CancellationTokenSource();
      _baseStream = baseStream;
      _execution = _baseStream.Add(self => Update(self.DeltaTime), _subscriptionHandle.Token);
      _execution.Priority = _priority;
      _baseStream.OnTerminate(Dispose, _subscriptionHandle.Token);
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

      if (RunningStream == this || RunningStream == other)
        throw new StreamsException($"Cannot join a running stream ({RunningStream})");

      CopyFrom(other);
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
      _subscriptionHandle?.Cancel();
      _subscriptionHandle = new CancellationTokenSource();

      _baseStream = stream;
      _execution = _baseStream.Add(self => Update(self.DeltaTime), _subscriptionHandle.Token).SetTickRate(_tickRate);
      _baseStream.OnTerminate(Dispose, _subscriptionHandle.Token);

      if (_delta.HasValue)
        _execution.SetDelta(_delta.Value);
      if (priority != null)
        _execution.Priority = _priority;
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

      _subscriptionHandle.Cancel();
      _subscriptionHandle = null;
      _execution = null;
      Terminate();
    }

  }

}