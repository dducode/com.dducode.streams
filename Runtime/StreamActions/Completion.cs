using System;

namespace StreamsForUnity.StreamActions {

  internal class Completion : ICompletable {

    public event Action OnComplete {
      add => _completeCallbacks += value ?? throw new ArgumentNullException();
      remove => _completeCallbacks -= value ?? throw new ArgumentNullException();
    }

    private Action _completeCallbacks;

    public void Complete() {
      try {
        _completeCallbacks?.Invoke();
      }
      finally {
        _completeCallbacks = null;
      }
    }

  }

}