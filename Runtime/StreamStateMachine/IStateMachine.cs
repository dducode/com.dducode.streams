using System;

namespace Streams.StreamStateMachine {

  public interface IStateMachine : IDisposable {

    public void SetState<TState>() where TState : State;

  }

}