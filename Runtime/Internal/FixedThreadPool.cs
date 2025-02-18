using System;
using System.Collections.Generic;
using System.Threading;
using JetBrains.Annotations;
using UnityEngine;

namespace StreamsForUnity.Internal {

  internal static class FixedThreadPool {

    private static readonly Thread[] _threads;
    private static readonly object _lock;
    private static readonly Queue<Action> _tasks;
    private static bool _isRunning;

    static FixedThreadPool() {
      _threads = new Thread[Environment.ProcessorCount];
      _lock = new object();
      _tasks = new Queue<Action>();
      _isRunning = true;

      for (var i = 0; i < _threads.Length; i++) {
        _threads[i] = new Thread(ThreadFunction) { IsBackground = true };
        _threads[i].Start();
      }

      Application.quitting += Shutdown;
    }

    public static void QueueWorkItem([NotNull] Action action) {
      if (action == null)
        throw new ArgumentNullException(nameof(action));

      lock (_lock) {
        _tasks.Enqueue(action);
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
        Action task;

        lock (_lock) {
          while (_tasks.Count == 0 && _isRunning)
            Monitor.Wait(_lock);

          if (!_isRunning && _tasks.Count == 0)
            return;

          task = _tasks.Dequeue();
        }

        task.Invoke();
      }
    }

  }

}