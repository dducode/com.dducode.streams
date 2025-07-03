using System;
using JetBrains.Annotations;

namespace Streams.StreamActions {

  public interface ICompletable {

    public bool IsCompleted { get; }

  }

  public interface ICallbackCompletable : ICompletable {

    public void OnComplete([NotNull] Action onComplete, StreamToken subscriptionToken = default);

  }

}