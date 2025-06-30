using System;

namespace Streams.StreamActions {

  public interface IConfigurable {

    public uint Priority { get; }
    public event Action OnPriorityChanged;
    public IConfigurable SetDelta(float value);
    public IConfigurable ResetDelta();
    public IConfigurable SetTickRate(uint value);
    public IConfigurable SetPriority(uint value);
    void Lock(StreamToken lockToken);

  }

}