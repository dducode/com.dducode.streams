using System;

namespace StreamsForUnity.StreamActions {

  public interface ICompletable {

    public event Action OnComplete;

  }

}