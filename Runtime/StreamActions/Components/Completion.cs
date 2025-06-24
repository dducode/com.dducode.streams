using System;
using System.Threading;

namespace Streams.StreamActions.Components {

  internal class Completion : ICompletable {

    private Action _completeCallbacks;
    private bool _completed;

    public void OnComplete(Action onComplete, CancellationToken subscriptionToken = default) {
      if (onComplete == null)
        throw new ArgumentNullException(nameof(onComplete));

      if (subscriptionToken.IsCancellationRequested)
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