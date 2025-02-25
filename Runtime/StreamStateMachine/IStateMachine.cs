using System;

namespace StreamsForUnity.StreamStateMachine {

  public interface IStateMachine : IDisposable {

    public void SetState<TState>() where TState : State;

  }

}