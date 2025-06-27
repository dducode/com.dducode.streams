using System;
using System.Threading;
using JetBrains.Annotations;
using Streams.StreamTasks;

namespace Streams {

  public readonly struct StreamToken {

    public static StreamToken None => default;
    public bool Released => _source?.Released ?? false;

    private readonly StreamTokenSource _source;

    public StreamToken(StreamTokenSource source) {
      _source = source;
    }

    public void Register([NotNull] Action action) {
      if (action == null)
        throw new ArgumentNullException(nameof(action));

      if (Released) {
        action();
        return;
      }

      _source?.Register(action);
    }

    internal void Register(ITask task) {
      if (Released) {
        if (!task.IsCompleted)
          task.SetCanceled();
        return;
      }

      _source?.Register(task);
    }

    public static implicit operator StreamToken(CancellationToken cancellationToken) {
      var source = new StreamTokenSource();
      cancellationToken.Register(source.Release);
      return source.Token;
    }

  }

}