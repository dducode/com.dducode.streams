namespace Streams.StreamActions {

  internal abstract class StreamActionBase : IInvokable {

    private static int NextId => ++_nextId;
    private static int _nextId = -1;

    public string Name { get; }
    internal int Id { get; } = NextId;

    private bool _cancellationRequested;

    private protected StreamActionBase(StreamToken cancellationToken) {
      cancellationToken.Register(() => _cancellationRequested = true);
      Name = GetType().Name;
    }

    public override string ToString() {
      return Name;
    }

    public virtual void Invoke(float deltaTime) {
      if (_cancellationRequested)
        throw new ActionCanceledException();
    }

  }

}