using System;
using System.Threading;
using JetBrains.Annotations;

namespace Streams {

  public readonly struct StreamToken {

    public static StreamToken None => new();
    public bool Released => _source?.Released ?? false;

    private readonly StreamTokenSource _source;

    public StreamToken(StreamTokenSource source) {
      _source = source;
    }

    public void Register([NotNull] Action action) {
      if (action == null)
        throw new ArgumentNullException(nameof(action));
      _source?.Register(action);
    }

    public static implicit operator StreamToken(CancellationToken cancellationToken) {
      var source = new StreamTokenSource();
      cancellationToken.Register(source.Release);
      return source.Token;
    }

  }

}