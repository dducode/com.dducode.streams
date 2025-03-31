using System.Threading.Tasks;
using NUnit.Framework;
using Streams.StreamStateMachine;
using Streams.Tests.Attributes;
using UnityEngine.PlayerLoop;

namespace Streams.Tests.StateMachineTests {

  public class StateMachineTests {

    [Test, Common]
    public async Task OnUpdateStateMachineTest() {
      var disposeSource = new StreamTokenSource();

      var finalState = new FinalState();
      var fsm = new StateMachine<Update>(new EntryState(), new HelloState(), finalState);
      disposeSource.Token.Register(fsm.Dispose);

      while (fsm.CurrentState != finalState)
        await Task.Yield();
      disposeSource.Release();
    }

  }

}