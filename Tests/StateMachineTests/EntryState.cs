using Streams.StreamStateMachine;
using UnityEngine;

namespace Streams.Tests.StateMachineTests {

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