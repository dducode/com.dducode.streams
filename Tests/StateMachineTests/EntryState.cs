using StreamsForUnity.StreamStateMachine;
using UnityEngine;

namespace StreamsForUnity.Tests.StateMachineTests {

  public class EntryState : State {

    protected override void OnEnter(StreamToken subscriptionToken) {
      Debug.Log("Entered EntryState");
      StateMachine.SetState<HelloState>();
    }

    protected override void OnExit() {
      Debug.Log("Exited EntryState");
    }

  }

}