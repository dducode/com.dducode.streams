using System;
using JetBrains.Annotations;

namespace Streams.StreamActions {

  public interface ICompletable {

    public void OnComplete([NotNull] Action onComplete, StreamToken subscriptionToken = default);

  }

}