using System.Threading;
using Streams.StreamStateMachine;
using UnityEngine;

namespace Streams.Tests.StateMachineTests {

  public class FinalState : State {

    protected override void OnEnter(CancellationToken subscriptionToken) {
      Debug.Log("Entered FinalState");
    }

    protected override void OnExit() {
      Debug.Log("Exited FinalState");
    }

  }

}