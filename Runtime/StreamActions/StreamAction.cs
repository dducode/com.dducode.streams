using System;
using System.Threading;
using JetBrains.Annotations;

namespace Streams.StreamActions {

  public abstract class StreamAction {

    private static int NextId => ++_nextId;
    private static int _nextId = -1;

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
      set {
        _priority = value;
        OnPriorityChanged?.Invoke();
      }
    }

    internal int Id { get; } = NextId;

    internal event Action OnPriorityChanged;
    private protected abstract Delegate Action { get; }
    private string ActionName => Action.Method.Name;

    private Action _cancelCallbacks;
    private string _name;
    private bool _canceled;
    private uint _priority;

    private protected StreamAction(CancellationToken cancellationToken) {
      cancellationToken.Register(() => _canceled = true);
      _priority = uint.MaxValue;
      _name = GetType().Name;
    }

    public void OnCancel([NotNull] Action onCancel, CancellationToken subscriptionToken = default) {
      if (onCancel == null)
        throw new ArgumentNullException(nameof(onCancel));

      if (subscriptionToken.IsCancellationRequested)
        return;

      if (_canceled) {
        onCancel();
        return;
      }

      _cancelCallbacks += onCancel;
      subscriptionToken.Register(() => _cancelCallbacks -= onCancel);
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