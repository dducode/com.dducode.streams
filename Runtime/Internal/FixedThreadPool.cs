using System;
using System.Collections.Generic;
using System.Threading;
using JetBrains.Annotations;
using UnityEngine;

namespace Streams.Internal {

  internal static class FixedThreadPool {

    public static int AvailableThreads => _threads.Length;

    private static readonly Thread[] _threads;
    private static readonly object _lock;
    private static readonly Queue<WorkSet> _workSets;
    private static bool _isRunning;

    static FixedThreadPool() {
      _threads = new Thread[Environment.ProcessorCount];
      _lock = new object();
      _workSets = new Queue<WorkSet>();
      _isRunning = true;

      for (var i = 0; i < _threads.Length; i++) {
        _threads[i] = new Thread(ThreadFunction) { IsBackground = true };
        _threads[i].Start();
      }

      Application.quitting += Shutdown;
    }

    public static void QueueWorkItem([NotNull] Action<WorkerState> task, [NotNull] WorkerState state) {
      if (task == null)
        throw new ArgumentNullException(nameof(task));
      if (state == null)
        throw new ArgumentNullException(nameof(state));

      lock (_lock) {
        _workSets.Enqueue(new WorkSet(task, state));
        Monitor.Pulse(_lock);
      }
    }

    private static void Shutdown() {
      lock (_lock) {
        _isRunning = false;
        Monitor.PulseAll(_lock);
      }

      foreach (Thread thread in _threads)
        thread.Join();
    }

    private static void ThreadFunction() {
      while (true) {
        WorkSet set;

        lock (_lock) {
          while (_workSets.Count == 0 && _isRunning)
            Monitor.Wait(_lock);

          if (!_isRunning && _workSets.Count == 0)
            return;

          set = _workSets.Dequeue();
        }

        set.Task.Invoke(set.State);
      }
    }

    private readonly struct WorkSet {

      public readonly Action<WorkerState> Task;
      public readonly WorkerState State;

      public WorkSet(Action<WorkerState> task, WorkerState state) {
        Task = task;
        State = state;
      }

    }

  }

}