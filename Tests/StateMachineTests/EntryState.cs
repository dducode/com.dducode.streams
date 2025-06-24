using System.Threading;
using Streams.StreamStateMachine;
using UnityEngine;

namespace Streams.Tests.StateMachineTests {

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