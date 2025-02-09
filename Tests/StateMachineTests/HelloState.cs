using System.Threading;
using StreamsForUnity.StreamStateMachine;
using UnityEngine;

namespace StreamsForUnity.Tests.StateMachineTests {

  public class HelloState : State {

    protected override void OnInitialize() {
      Debug.Log("Hello World!");
    }

    protected override void OnEnter(CancellationToken subscriptionToken) {
      Debug.Log("Entered HelloState");
      Stream.AddTimer(2, () => StateMachine.SetState<FinalState>(), subscriptionToken);
    }

    protected override void OnExit() {
      Debug.Log("Exited HelloState");
    }

  }

}