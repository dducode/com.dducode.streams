namespace StreamsForUnity.StreamStateMachine {

  public interface IStateMachine {

    public void SetState<TState>() where TState : State;

  }

}