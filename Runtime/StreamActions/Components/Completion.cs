using System;

namespace Streams.StreamActions.Components {

  internal class Completion : ICompletable {

    private Action _completeCallbacks;
    private bool _isCompleted;

    public void OnComplete(Action onComplete, StreamToken subscriptionToken = default) {
      if (onComplete == null)
        throw new ArgumentNullException(nameof(onComplete));

      if (subscriptionToken.Released)
        return;

      if (_isCompleted) {
        onComplete();
        return;
      }

      _completeCallbacks += onComplete;
      subscriptionToken.Register(() => _completeCallbacks -= onComplete);
    }

    public void Complete() {
      if (_isCompleted)
        return;

      try {
        _completeCallbacks?.Invoke();
      }
      finally {
        _isCompleted = true;
        _completeCallbacks = null;
      }
    }

  }

}