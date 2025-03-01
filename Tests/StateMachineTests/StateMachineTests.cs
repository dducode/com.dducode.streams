using System.Threading.Tasks;
using NUnit.Framework;
using StreamsForUnity.StreamStateMachine;
using StreamsForUnity.Tests.Attributes;
using UnityEngine.PlayerLoop;

namespace StreamsForUnity.Tests.StateMachineTests {

  public class StateMachineTests {

    [Test, Common]
    public async Task OnUpdateStateMachineTest() {
      var disposeSource = new StreamTokenSource();

      var finalState = new FinalState();
      var fsm = new StateMachine<Update>(new EntryState(), new HelloState(), finalState);
      disposeSource.Register(fsm.Dispose);

      while (fsm.CurrentState != finalState)
        await Task.Yield();
      disposeSource.Release();
    }

  }

}