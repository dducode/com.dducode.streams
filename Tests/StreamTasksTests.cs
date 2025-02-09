using System.Threading.Tasks;
using NUnit.Framework;
using StreamsForUnity.StreamTasks;
using StreamsForUnity.StreamTasks.Extensions;
using UnityEngine;
using UnityEngine.PlayerLoop;

namespace StreamsForUnity.Tests {

  public class StreamTasksTests {

    [Test]
    public async Task AsyncActionTest() {
      var tcs = new TaskCompletionSource<bool>();
      Streams.Get<Update.ScriptRunBehaviourUpdate>().AddOnce(async () => {
        Debug.Log(1);
        await StreamTask.Yield();
        Debug.Log(2);
        await StreamTask.Delay(1000);
        Debug.Log(3);
        tcs.SetResult(true);
      });
      Assert.IsTrue(await tcs.Task);
    }

    [Test]
    public void ErrorTest() {
      try {
        StreamTask.Yield();
      }
      catch (StreamsException exception) {
        Debug.Log($"StreamsException was thrown; message: <b>{exception.Message}</b>");
        return;
      }

      Assert.Fail();
    }

    [Test]
    public async Task ContinuationTest() {
      var tcs = new TaskCompletionSource<bool>();
      Streams.Get<Update.ScriptRunBehaviourUpdate>().AddOnce(async () => {
        await Task.Delay(1000).ToStreamTask();
        tcs.SetResult(true);
      });
      Assert.IsTrue(await tcs.Task);
    }

  }

}