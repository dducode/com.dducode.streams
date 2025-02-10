using System.Threading.Tasks;
using NUnit.Framework;
using StreamsForUnity.StreamTasks;
using UnityEngine;
using UnityEngine.PlayerLoop;

namespace StreamsForUnity.Tests {

  public class StreamTasksTests {

    [Test]
    public async Task AsyncActionTest() {
      var tcs = new TaskCompletionSource<bool>();
      SetFailureAfterTime(2, tcs);
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
    public async Task WaitWhileTest() {
      var tcs = new TaskCompletionSource<bool>();
      SetFailureAfterTime(2, tcs);
      var flag = false;
      ExecutionStream stream = Streams.Get<Update.ScriptRunBehaviourUpdate>();
      stream.AddTimer(1, () => flag = true);
      stream.AddOnce(async () => {
        await StreamTask.WaitWhile(() => !flag);
        tcs.SetResult(true);
      });
      Assert.IsTrue(await tcs.Task);
    }

    [Test]
    public async Task WhenAllTest() {
      var tcs = new TaskCompletionSource<bool>();
      int firstDelay = Random.Range(100, 1000);
      int secondDelay = Random.Range(100, 1000);
      int thirdDelay = Random.Range(100, 1000);
      Debug.Log($"First delay: {firstDelay}");
      Debug.Log($"Second delay: {secondDelay}");
      Debug.Log($"Third delay: {thirdDelay}");

      SetFailureAfterTime(2, tcs);
      Streams.Get<Update.ScriptRunBehaviourUpdate>().AddOnce(async () => {
        await StreamTask.WhenAll(StreamTask.Delay(firstDelay), StreamTask.Delay(secondDelay), StreamTask.Delay(thirdDelay));
        tcs.SetResult(true);
      });
      Assert.IsTrue(await tcs.Task);
    }

    [Test]
    public async Task WhenAnyTest() {
      var tcs = new TaskCompletionSource<bool>();
      int firstDelay = Random.Range(100, 1000);
      int secondDelay = Random.Range(100, 1000);
      int thirdDelay = Random.Range(100, 1000);
      Debug.Log($"First delay: {firstDelay}");
      Debug.Log($"Second delay: {secondDelay}");
      Debug.Log($"Third delay: {thirdDelay}");

      SetFailureAfterTime(2, tcs);
      Streams.Get<Update.ScriptRunBehaviourUpdate>().AddOnce(async () => {
        await StreamTask.WhenAny(StreamTask.Delay(firstDelay), StreamTask.Delay(secondDelay), StreamTask.Delay(thirdDelay));
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
      SetFailureAfterTime(2, tcs);
      Streams.Get<Update.ScriptRunBehaviourUpdate>().AddOnce(async () => {
        await StreamTask.Delay(1000).ContinueWith(() => tcs.SetResult(true));
      });
      Assert.IsTrue(await tcs.Task);
    }

    [Test]
    public async Task AsyncContinuationTest() {
      var tcs = new TaskCompletionSource<bool>();
      SetFailureAfterTime(3, tcs);
      Streams.Get<Update.ScriptRunBehaviourUpdate>().AddOnce(async () => {
        await StreamTask.Delay(1000).ContinueWith(async () => {
          await StreamTask.Delay(1000).ContinueWith(() => tcs.SetResult(true));
        });
      });
      Assert.IsTrue(await tcs.Task);
    }

    [Test]
    public async Task NestedContinuationsTest() {
      var tcs = new TaskCompletionSource<bool>();
      SetFailureAfterTime(6, tcs);
      Streams.Get<Update.ScriptRunBehaviourUpdate>().AddOnce(async () => {
        await StreamTask.Delay(1000).ContinueWith(async () => {
          await StreamTask.Delay(1000).ContinueWith(async () => {
            await StreamTask.Delay(1000);
          }).ContinueWith(async () => await StreamTask.Delay(1000));
        }).ContinueWith(async () => await StreamTask.Delay(1000)).ContinueWith(() => tcs.SetResult(true));
      });
      Assert.IsTrue(await tcs.Task);
    }

    private void SetFailureAfterTime(float time, TaskCompletionSource<bool> tcs) {
      Streams.Get<Update.ScriptRunBehaviourUpdate>().AddTimer(time, () => tcs.SetResult(false));
    }

  }

}