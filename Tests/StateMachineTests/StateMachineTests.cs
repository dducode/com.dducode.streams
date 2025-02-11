using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using StreamsForUnity.StreamStateMachine;
using StreamsForUnity.Tests.Attributes;
using UnityEngine.PlayerLoop;

namespace StreamsForUnity.Tests.StateMachineTests {

  public class StateMachineTests {

    [Test, Common]
    public async Task OnUpdateStateMachineTest() {
      var disposeSource = new CancellationTokenSource();

      var finalState = new FinalState();
      var fsm = new StateMachine<Update.ScriptRunBehaviourUpdate>(disposeSource.Token, new EntryState(), new HelloState(), finalState);
      fsm.SetState<EntryState>();

      while (fsm.CurrentState != finalState)
        await Task.Yield();
      disposeSource.Cancel();
    }

  }

}