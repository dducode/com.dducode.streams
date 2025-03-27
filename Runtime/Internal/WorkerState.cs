using System;
using System.Threading;

namespace Streams.Internal {

  internal class WorkerState {

    public int iterations;
    public float deltaTime;
    public int nextItem;
    public Action<float, int> body;
    public int workersCount;
    public ManualResetEventSlim workEvent;

  }

}