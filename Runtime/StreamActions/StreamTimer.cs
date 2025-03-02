using System;

namespace StreamsForUnity.StreamActions {

  public sealed class StreamTimer : StreamAction, ICompletable {

    public event Action OnComplete {
      add => _completion.OnComplete += value;
      remove => _completion.OnComplete -= value;
    }

    private protected override Delegate Action => _action;

    private readonly Action _action;
    private readonly Completion _completion = new();
    private float _remainingTime;

    internal StreamTimer(float time, Action action, StreamToken cancellationToken) : base(cancellationToken, uint.MaxValue) {
      _action = action;
      _remainingTime = time;
    }

    internal override void Invoke(float deltaTime) {
      if (Canceled())
        return;

      _remainingTime = Math.Max(0, _remainingTime - deltaTime);

      if (_remainingTime == 0) {
        _action();
        _completion.Complete();
      }
    }

  }

}