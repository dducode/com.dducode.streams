using System;

namespace Streams.StreamActions {

  public abstract class StreamActionBase {

    private static int NextId => ++_nextId;
    private static int _nextId = -1;

    public string Name { get; }
    internal int Id { get; } = NextId;

    private protected abstract Delegate Action { get; }

    private string ActionName => Action.Method.Name;
    private bool _cancellationRequested;

    private protected StreamActionBase(StreamToken cancellationToken) {
      cancellationToken.Register(() => _cancellationRequested = true);
      Name = GetType().Name;
    }

    public override string ToString() {
      return $"{Name} ({ActionName})";
    }

    internal virtual void Invoke(float deltaTime) {
      if (_cancellationRequested)
        throw new ActionCanceledException();
    }

  }

}