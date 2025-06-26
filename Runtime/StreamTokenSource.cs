using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using Streams.StreamTasks;

namespace Streams {

  public class StreamTokenSource : IDisposable {

    public StreamToken Token { get; }
    public bool Released { get; private set; }

    private readonly Queue<Action> _onReleaseActions = new();
    private readonly Queue<ITask> _cancellableTasks = new();

    public StreamTokenSource() {
      Token = new StreamToken(this);
    }

    public void Release() {
      if (Released)
        return;
      Released = true;

      while (_onReleaseActions.TryDequeue(out Action action))
        action();

      while (_cancellableTasks.TryDequeue(out ITask task))
        if (!task.IsCompleted)
          task.SetCanceled();
    }

    internal void Register([NotNull] Action onReleaseAction) {
      if (onReleaseAction == null)
        throw new ArgumentNullException(nameof(onReleaseAction));

      if (Released) {
        onReleaseAction();
        return;
      }

      _onReleaseActions.Enqueue(onReleaseAction);
    }

    internal void Register(ITask task) {
      if (task == null)
        throw new ArgumentNullException(nameof(task));

      if (Released) {
        if (!task.IsCompleted)
          task.SetCanceled();
        return;
      }

      _cancellableTasks.Enqueue(task);
    }

    public void Dispose() {
      Release();
    }

  }

}