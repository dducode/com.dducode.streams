using System.Threading;

namespace StreamsForUnity.Internal {

  internal class ActionLifecycle {

    internal float remainingTime;
    internal readonly CancellationToken token;

    internal ActionLifecycle(float remainingTime, CancellationToken token) {
      this.remainingTime = remainingTime;
      this.token = token;
    }

  }

}