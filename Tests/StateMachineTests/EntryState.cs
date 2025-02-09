using System.Threading;
using StreamsForUnity.StreamStateMachine;
using UnityEngine;

namespace StreamsForUnity.Tests.StateMachineTests {

  public class EntryState : State {

    protected override void OnEnter(CancellationToken subscriptionToken) {
      Debug.Log("Entered EntryState");
      StateMachine.SetState<HelloState>();
    }

    protected override void OnExit() {
      Debug.Log("Exited EntryState");
    }

  }

}