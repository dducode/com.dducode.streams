using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NUnit.Framework;
using Streams.Exceptions;
using Streams.StreamTasks;
using Streams.StreamTasks.Extensions;
using Streams.Tests.Attributes;
using UnityEngine;
using UnityEngine.PlayerLoop;
using Random = UnityEngine.Random;

namespace Streams.Tests {

  public class StreamTasksTests {

    [Test, StreamTasks]
    public async Task AsyncActionTest() {
      var tcs = new TaskCompletionSource<bool>();
      SetFailureAfterTime(2, tcs);
      UnityPlayerLoop.GetStream<Update>().AddOnce(async () => {
        Debug.Log(1);
        await StreamTask.Yield();
        Debug.Log(2);
        await StreamTask.Delay(1000);
        Debug.Log(3);
        tcs.SetResult(true);
      });
      Assert.IsTrue(await tcs.Task);
    }

    [Test, StreamTasks]
    public async Task WaitWhileTest() {
      var tcs = new TaskCompletionSource<bool>();
      SetFailureAfterTime(2, tcs);
      var flag = false;
      ExecutionStream stream = UnityPlayerLoop.GetStream<Update>();
      stream.AddTimer(1, () => flag = true);
      stream.AddOnce(async () => {
        await StreamTask.WaitWhile(() => !flag);
        tcs.SetResult(true);
      });
      Assert.IsTrue(await tcs.Task);
    }

    [Test, StreamTasks]
    public async Task WhenAllTest() {
      var tcs = new TaskCompletionSource<bool>();
      int firstDelay = Random.Range(100, 1000);
      int secondDelay = Random.Range(100, 1000);
      int thirdDelay = Random.Range(100, 1000);
      Debug.Log($"First delay: {firstDelay}");
      Debug.Log($"Second delay: {secondDelay}");
      Debug.Log($"Third delay: {thirdDelay}");

      SetFailureAfterTime(2, tcs);
      UnityPlayerLoop.GetStream<Update>().AddOnce(async () => {
        await StreamTask.WhenAll(StreamTask.Delay(firstDelay), StreamTask.Delay(secondDelay), StreamTask.Delay(thirdDelay));
        tcs.SetResult(true);
      });
      Assert.IsTrue(await tcs.Task);
    }

    [Test, StreamTasks]
    public async Task WhenAnyTest() {
      var tcs = new TaskCompletionSource<bool>();
      int firstDelay = Random.Range(100, 1000);
      int secondDelay = Random.Range(100, 1000);
      int thirdDelay = Random.Range(100, 1000);
      Debug.Log($"First delay: {firstDelay}");
      Debug.Log($"Second delay: {secondDelay}");
      Debug.Log($"Third delay: {thirdDelay}");

      SetFailureAfterTime(2, tcs);
      UnityPlayerLoop.GetStream<Update>().AddOnce(async () => {
        await StreamTask.WhenAny(StreamTask.Delay(firstDelay), StreamTask.Delay(secondDelay), StreamTask.Delay(thirdDelay));
        tcs.SetResult(true);
      });
      Assert.IsTrue(await tcs.Task);
    }

    [Test, StreamTasks]
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

    [Test, StreamTasks]
    public async Task ContinuationTest() {
      var tcs = new TaskCompletionSource<bool>();
      SetFailureAfterTime(2, tcs);
      UnityPlayerLoop.GetStream<Update>().AddOnce(async () => {
        await StreamTask.Delay(1000).ContinueWith(() => tcs.SetResult(true));
      });
      Assert.IsTrue(await tcs.Task);
    }

    [Test, StreamTasks]
    public async Task AsyncContinuationTest() {
      var tcs = new TaskCompletionSource<bool>();
      SetFailureAfterTime(3, tcs);
      UnityPlayerLoop.GetStream<Update>().AddOnce(async () => {
        await StreamTask.Delay(1000).ContinueWith(async () => {
          await StreamTask.Delay(1000).ContinueWith(() => tcs.SetResult(true));
        });
      });
      Assert.IsTrue(await tcs.Task);
    }

    [Test, StreamTasks]
    public async Task NestedContinuationsTest() {
      var tcs = new TaskCompletionSource<bool>();
      SetFailureAfterTime(6, tcs);
      UnityPlayerLoop.GetStream<Update>().AddOnce(async () => {
        await StreamTask.Delay(1000).ContinueWith(async () => {
          await StreamTask.Delay(1000).ContinueWith(async () => {
            await StreamTask.Delay(1000);
          }).ContinueWith(async () => await StreamTask.Delay(1000));
        }).ContinueWith(async () => await StreamTask.Delay(1000)).ContinueWith(() => tcs.SetResult(true));
      });
      Assert.IsTrue(await tcs.Task);
    }

    [Test, StreamTasks]
    public async Task MultipleContinuationsTest() {
      var tcs = new TaskCompletionSource<bool>();
      var tasks = new List<StreamTask>(5);
      SetFailureAfterTime(3, tcs);

      UnityPlayerLoop.GetStream<Update>().AddOnce(async () => {
        StreamTask task = StreamTask.Delay(1000);

        for (var i = 0; i < 5; i++) {
          int index = i;
          tasks.Add(task.ContinueWith(() => Debug.Log(index)));
        }

        await StreamTask.WhenAll(tasks);
        tcs.SetResult(true);
      });

      Assert.IsTrue(await tcs.Task);
    }

    [Test, StreamTasks]
    public async Task MultipleAsyncContinuationsTest() {
      var tcs = new TaskCompletionSource<bool>();
      var tasks = new List<StreamTask>(5);
      SetFailureAfterTime(3, tcs);

      UnityPlayerLoop.GetStream<Update>().AddOnce(async () => {
        StreamTask task = StreamTask.Delay(1000);

        for (var i = 0; i < 5; i++) {
          int randomTime = Random.Range(100, 1000);
          Debug.Log($"Unit {i}, time: {randomTime}");
          int index = i;
          tasks.Add(task.ContinueWith(async () => {
            await StreamTask.Delay(randomTime);
            Debug.Log(index);
          }));
        }

        await StreamTask.WhenAll(tasks);
        tcs.SetResult(true);
      });

      Assert.IsTrue(await tcs.Task);
    }

    [Test, StreamTasks]
    public async Task ImmediateCancellationTest() {
      var tcs = new TaskCompletionSource<bool>();
      var sts = new StreamTokenSource();
      SetFailureAfterTime(2, tcs);

      UnityPlayerLoop.GetStream<Update>().AddOnce(async () => {
        try {
          sts.Release();
          await StreamTask.Delay(1000, sts.Token);
          tcs.SetResult(false);
        }
        catch (OperationCanceledException) {
          tcs.SetResult(true);
        }
      });

      Assert.IsTrue(await tcs.Task);
    }

    [Test, StreamTasks]
    public async Task DelayedCancellationTest() {
      var tcs = new TaskCompletionSource<bool>();
      var sts = new StreamTokenSource();
      SetFailureAfterTime(2, tcs);

      UnityPlayerLoop.GetStream<Update>().AddOnce(async () => {
        try {
          await StreamTask.Delay(1000, sts.Token);
          tcs.SetResult(false);
        }
        catch (OperationCanceledException) {
          tcs.SetResult(true);
        }
      });
      UnityPlayerLoop.GetStream<Update>().AddTimer(0.5f, () => sts.Release());

      Assert.IsTrue(await tcs.Task);
    }

    [Test, StreamTasks]
    public async Task ErrorInsideAsyncMethodTest() {
      var tcs = new TaskCompletionSource<bool>();
      SetFailureAfterTime(2, tcs);

      UnityPlayerLoop.GetStream<Update>().AddOnce(async () => {
        try {
          await Task.Delay(500);
          await StreamTask.Delay(500);
          tcs.SetResult(false);
        }
        catch (StreamsException exception) {
          Debug.Log($"StreamsException was thrown; message: <b>{exception.Message}</b>");
          tcs.SetResult(true);
        }
      });

      Assert.IsTrue(await tcs.Task);
    }

    [Test, StreamTasks]
    public async Task ToStreamTaskTest() {
      var tcs = new TaskCompletionSource<bool>();
      SetFailureAfterTime(2, tcs);

      UnityPlayerLoop.GetStream<Update>().AddOnce(async () => {
        await Task.Delay(500).ToStreamTask();
        await StreamTask.Delay(500);
        tcs.SetResult(true);
      });

      Assert.IsTrue(await tcs.Task);
    }

    [Test, StreamTasks]
    public async Task AsyncOnCompleteTest() {
      var tcs = new TaskCompletionSource<bool>();
      SetFailureAfterTime(1, tcs);

      UnityPlayerLoop.GetStream<Update>().AddOnce(async () => {
        await StreamTask.Delay(500);
      }).OnComplete(() => tcs.SetResult(true));

      Assert.IsTrue(await tcs.Task);
    }

    private void SetFailureAfterTime(float time, TaskCompletionSource<bool> tcs) {
      UnityPlayerLoop.GetStream<Update>().AddTimer(time, () => tcs.SetResult(false));
    }

  }

}