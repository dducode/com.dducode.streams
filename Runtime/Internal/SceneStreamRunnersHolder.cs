using System;
using System.Collections.Generic;
using System.Threading;
using StreamsForUnity.StreamRunners;

namespace StreamsForUnity.Internal {

  internal class SceneStreamRunnersHolder {

    private readonly Dictionary<Type, IStreamRunner> _runners = new();
    private readonly CancellationTokenSource _disposeHandle = new();

    internal void AddStreamRunner<TBaseSystem>(StreamRunner<TBaseSystem> runner, StreamTokenSource disposeHandle) {
      _runners.Add(typeof(TBaseSystem), runner);
      _disposeHandle.Token.Register(disposeHandle.Release);
    }

    internal bool TryGetStream<TBaseSystem>(out ExecutionStream executionStream) {
      if (_runners.TryGetValue(typeof(TBaseSystem), out IStreamRunner runner)) {
        executionStream = runner.Stream;
        return true;
      }

      executionStream = null;
      return false;
    }

    internal void ReorderRunners(uint priority) {
      foreach (IStreamRunner runner in _runners.Values)
        runner.ChangePriority(priority);
    }

    internal void DisposeAttachedRunners() {
      _disposeHandle.Cancel();
      _runners.Clear();
    }

  }

}