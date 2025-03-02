using System;

namespace StreamsForUnity.StreamActions {

  public abstract class StreamAction {

    private static int NextId => ++_nextId;
    private static int _nextId = -1;

    public int Id { get; } = NextId;

    public string Name {
      get => _name;
      set {
        if (string.IsNullOrEmpty(value))
          throw new ArgumentNullException(nameof(Name));
        _name = value;
      }
    }

    public uint Priority {
      get => _priority;
      internal set {
        _priority = value;
        OnPriorityChanged?.Invoke();
      }
    }

    public event Action OnCancel {
      add => _cancelCallbacks += value ?? throw new ArgumentNullException(nameof(value));
      remove => _cancelCallbacks -= value ?? throw new ArgumentNullException(nameof(value));
    }

    internal event Action OnPriorityChanged;
    private protected abstract Delegate Action { get; }
    private string ActionName => Action.Method.Name;

    private Action _cancelCallbacks;
    private string _name = nameof(StreamAction);
    private bool _canceled;
    private uint _priority;

    private protected StreamAction(StreamToken cancellationToken, uint priority) {
      cancellationToken.Register(() => _canceled = true);
      _priority = priority;
    }

    public override string ToString() {
      return $"{Name} ({ActionName})";
    }

    internal abstract void Invoke(float deltaTime);

    private protected bool Canceled() {
      if (_canceled) {
        _cancelCallbacks?.Invoke();
        _cancelCallbacks = null;
        return true;
      }

      return false;
    }

  }

}