using System;

namespace Streams.StreamActions.Components {

  internal class Configuration {

    public float Delta {
      get => _delta ?? throw new ArgumentNullException(nameof(Delta));
      set {
        if (value <= 0)
          throw new ArgumentOutOfRangeException(nameof(value), "Delta should be positive");

        _tickRate = 1;
        _delta = value;
      }
    }

    public bool HasDelta => _delta.HasValue;

    public uint TickRate {
      get => _tickRate;
      set {
        if (value == 0)
          throw new ArgumentOutOfRangeException(nameof(value), "Tick rate cannot be zero");

        _delta = null;
        _tickRate = value;
      }
    }

    private float? _delta;
    private uint _tickRate = 1;

    public void ResetDelta() {
      _delta = null;
    }

  }

}