using System;
using JetBrains.Annotations;

namespace Streams.StreamActions {

  public interface ICompletable {

    public bool IsCompleted { get; }
    public void OnComplete([NotNull] Action onComplete, StreamToken subscriptionToken = default);

  }

}