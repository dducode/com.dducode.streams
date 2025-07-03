using System;

namespace Streams.StreamActions.Components {

  internal class Completion : ICallbackCompletable {

    public bool IsCompleted { get; private set; }

    private Action _completeCallbacks;

    public void OnComplete(Action onComplete, StreamToken subscriptionToken = default) {
      if (onComplete == null)
        throw new ArgumentNullException(nameof(onComplete));

      if (subscriptionToken.Released)
        return;

      if (IsCompleted) {
        onComplete();
        return;
      }

      _completeCallbacks += onComplete;
      subscriptionToken.Register(() => _completeCallbacks -= onComplete);
    }

    public void Complete() {
      if (IsCompleted)
        return;

      try {
        _completeCallbacks?.Invoke();
      }
      finally {
        IsCompleted = true;
        _completeCallbacks = null;
      }
    }

  }

}