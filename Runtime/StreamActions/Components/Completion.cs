using System;

namespace StreamsForUnity.StreamActions.Components {

  internal class Completion : ICompletable {

    private Action _completeCallbacks;
    private bool _completed;

    public void OnComplete(Action onComplete, StreamToken subscriptionToken = default) {
      if (onComplete == null)
        throw new ArgumentNullException(nameof(onComplete));

      if (subscriptionToken.Released)
        return;

      if (_completed) {
        onComplete();
        return;
      }

      _completeCallbacks += onComplete;
      subscriptionToken.Register(() => _completeCallbacks -= onComplete);
    }

    public void Complete() {
      if (_completed)
        return;

      _completed = true;

      try {
        _completeCallbacks?.Invoke();
      }
      finally {
        _completeCallbacks = null;
      }
    }

  }

}