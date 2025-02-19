using System;
using System.Threading;
using JetBrains.Annotations;

namespace StreamsForUnity.Internal {

  internal class ParallelActionsWorker {

    private readonly ManualResetEventSlim _workEvent = new(true);
    private readonly WorkerState _state = new();
    private readonly Action<WorkerState> _work;

    public ParallelActionsWorker() {
      _work = state => {
        int index;
        while ((index = Interlocked.Increment(ref state.nextItem) - 1) < state.iterations)
          state.body(state.deltaTime, index);

        if (Interlocked.Decrement(ref state.workersCount) == 0)
          state.workEvent.Set();
      };
    }

    public void Start(float deltaTime, int iterations, [NotNull] Action<float, int> body) {
      if (body == null)
        throw new ArgumentNullException(nameof(body));
      if (!_workEvent.IsSet)
        throw new StreamsException("Previous work is still running");

      int workersCount = Math.Min(iterations, Environment.ProcessorCount);

      _state.iterations = iterations;
      _state.deltaTime = deltaTime;
      _state.nextItem = 0;
      _state.body = body;
      _state.workersCount = workersCount;
      _state.workEvent = _workEvent;

      _workEvent.Reset();

      for (var i = 0; i < workersCount; i++)
        FixedThreadPool.QueueWorkItem(_work, _state);
    }

    public void Wait() {
      _workEvent.Wait();
    }

  }

}