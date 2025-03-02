using System;
using JetBrains.Annotations;

namespace StreamsForUnity.StreamActions {

  public interface ICompletable {

    public void OnComplete([NotNull] Action onComplete, StreamToken subscriptionToken = default);

  }

}