using System;
using System.Threading;
using JetBrains.Annotations;

namespace StreamsForUnity.Internal {

  internal class ParallelActionsWorker {

    private readonly ManualResetEventSlim _workEvent = new(true);

    public void Start(int iterations, [NotNull] Action<int> body) {
      if (body == null)
        throw new ArgumentNullException(nameof(body));
      if (!_workEvent.IsSet)
        throw new StreamsException("Previous work is still running");

      _workEvent.Reset();
      var nextItem = 0;
      int workersCount = Environment.ProcessorCount;

      WaitCallback work = _ => {
        int index;
        while ((index = Interlocked.Increment(ref nextItem) - 1) < iterations)
          body(index);

        if (Interlocked.Decrement(ref workersCount) == 0)
          _workEvent.Set();
      };

      int schedulerWorkersCount = workersCount;

      for (var i = 0; i < schedulerWorkersCount; i++)
        ThreadPool.QueueUserWorkItem(work);
    }

    public void Wait() {
      _workEvent.Wait();
    }

  }

}