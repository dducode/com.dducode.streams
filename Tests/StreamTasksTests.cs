using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NUnit.Framework;
using Streams.Exceptions;
using Streams.Extensions;
using Streams.StreamTasks;
using Streams.Tests.Attributes;
using UnityEngine;
using UnityEngine.PlayerLoop;
using Random = UnityEngine.Random;

namespace Streams.Tests {

  public class StreamTasksTests {

    [Test, StreamTasks]
    public async Task AsyncActionTest() {
      Debug.Log("Start AsyncActionTest");
      var tcs = new TaskCompletionSource<bool>();
      using var cts = new StreamTokenSource();

      SetFailureAfterTime(2, tcs, cts.Token);
      UnityPlayerLoop.GetStream<Update>().AddOnce(async () => {
        Debug.Log(1);
        await StreamTask.Yield();
        Debug.Log(2);
        await StreamTask.Delay(1000);
        Debug.Log(3);
        tcs.SetResult(true);
      });

      Assert.IsTrue(await tcs.Task);
      Debug.Log("End AsyncActionTest");
    }

    [Test, StreamTasks]
    public async Task AsyncContinuationTest() {
      Debug.Log("Start AsyncContinuationTest");
      var tcs = new TaskCompletionSource<bool>();
      using var cts = new StreamTokenSource();
      SetFailureAfterTime(3, tcs, cts.Token);
      UnityPlayerLoop.GetStream<Update>().AddOnce(async () => {
        try {
          await StreamTask.Delay(1000).ContinueWith(async () => {
            await StreamTask.Delay(1000).ContinueWith(() => tcs.SetResult(true));
          });
        }
        catch (InvalidOperationException exception) {
          Debug.LogError("Exception in test");
          Debug.LogException(exception);
          tcs.SetResult(false);
        }
      });
      Assert.IsTrue(await tcs.Task);
      Debug.Log("End AsyncContinuationTest");
    }

    [Test, StreamTasks]
    public async Task AsyncOnCompleteTest() {
      var tcs = new TaskCompletionSource<bool>();
      using var cts = new StreamTokenSource();
      SetFailureAfterTime(1, tcs, cts.Token);

      UnityPlayerLoop.GetStream<Update>().AddOnce(async () => {
        await StreamTask.Delay(500);
      }).OnComplete(() => tcs.SetResult(true));

      Assert.IsTrue(await tcs.Task);
    }

    [Test, StreamTasks]
    public async Task ContinuationTest() {
      var tcs = new TaskCompletionSource<bool>();
      using var cts = new StreamTokenSource();
      SetFailureAfterTime(2, tcs, cts.Token);
      UnityPlayerLoop.GetStream<Update>().AddOnce(async () => {
        await StreamTask.Delay(1000).ContinueWith(() => tcs.SetResult(true));
      });
      Assert.IsTrue(await tcs.Task);
    }

    [Test, StreamTasks]
    public async Task ContinueOnStreamTest() {
      var tcs = new TaskCompletionSource<bool>();
      using var cts = new StreamTokenSource();
      SetFailureAfterTime(1, tcs, cts.Token);

      UnityPlayerLoop.GetStream<Update>().AddOnce(async () => {
        Debug.Log($"Run on stream {ExecutionStream.RunningStream}");
        await StreamTask.ContinueOnStream<FixedUpdate>();
        Debug.Log($"Run on stream {ExecutionStream.RunningStream}");
        tcs.SetResult(true);
      });

      Assert.IsTrue(await tcs.Task);
    }

    [Test, StreamTasks]
    public async Task DelayedCancellationTest() {
      var tcs = new TaskCompletionSource<bool>();
      var sts = new StreamTokenSource();
      using var disposingSource = new StreamTokenSource();
      SetFailureAfterTime(2, tcs, disposingSource.Token);

      UnityPlayerLoop.GetStream<Update>().AddOnce(async () => {
        try {
          await StreamTask.Delay(1000).WithCancellation(sts.Token);
          tcs.SetResult(false);
        }
        catch (OperationCanceledException) {
          tcs.SetResult(true);
        }
      });
      UnityPlayerLoop.GetStream<Update>().AddDelayed(0.5f, () => sts.Release());

      Assert.IsTrue(await tcs.Task);
    }

    [Test, StreamTasks]
    public async Task ErrorInsideAsyncMethodTest() {
      var tcs = new TaskCompletionSource<bool>();
      using var cts = new StreamTokenSource();
      SetFailureAfterTime(2, tcs, cts.Token);

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
    public async Task ImmediateCancellationTest() {
      var tcs = new TaskCompletionSource<bool>();
      var sts = new StreamTokenSource();
      using var disposingSource = new StreamTokenSource();
      SetFailureAfterTime(2, tcs, disposingSource.Token);

      UnityPlayerLoop.GetStream<Update>().AddOnce(async () => {
        try {
          sts.Release();
          await StreamTask.Delay(1000).WithCancellation(sts.Token);
          tcs.SetResult(false);
        }
        catch (OperationCanceledException) {
          tcs.SetResult(true);
        }
      });

      Assert.IsTrue(await tcs.Task);
    }

    [Test, StreamTasks]
    public async Task MultipleAsyncContinuationsTest() {
      var tcs = new TaskCompletionSource<bool>();
      using var cts = new StreamTokenSource();
      var tasks = new List<StreamTask>(5);
      SetFailureAfterTime(3, tcs, cts.Token);

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

        await StreamTask.WhenAll(tasks.ToArray());
        tcs.SetResult(true);
      });

      Assert.IsTrue(await tcs.Task);
    }

    [Test, StreamTasks]
    public async Task MultipleContinuationsTest() {
      var tcs = new TaskCompletionSource<bool>();
      using var cts = new StreamTokenSource();
      var tasks = new List<StreamTask>(5);
      SetFailureAfterTime(3, tcs, cts.Token);

      UnityPlayerLoop.GetStream<Update>().AddOnce(async () => {
        StreamTask task = StreamTask.Delay(1000);

        for (var i = 0; i < 5; i++) {
          int index = i;
          tasks.Add(task.ContinueWith(() => Debug.Log(index)));
        }

        await StreamTask.WhenAll(tasks.ToArray());
        tcs.SetResult(true);
      });

      Assert.IsTrue(await tcs.Task);
    }

    [Test, StreamTasks]
    public async Task NestedContinuationsTest() {
      var tcs = new TaskCompletionSource<bool>();
      using var cts = new StreamTokenSource();
      SetFailureAfterTime(6, tcs, cts.Token);
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
    public async Task ToStreamTaskTest() {
      var tcs = new TaskCompletionSource<bool>();
      using var cts = new StreamTokenSource();
      SetFailureAfterTime(2, tcs, cts.Token);

      UnityPlayerLoop.GetStream<Update>().AddOnce(async () => {
        await Task.Delay(500).ToStreamTask();
        await StreamTask.Delay(500);
        tcs.SetResult(true);
      });

      Assert.IsTrue(await tcs.Task);
    }

    [Test, StreamTasks]
    public async Task WaitWhileTest() {
      var tcs = new TaskCompletionSource<bool>();
      using var cts = new StreamTokenSource();

      SetFailureAfterTime(2, tcs, cts.Token);
      var flag = false;
      ExecutionStream stream = UnityPlayerLoop.GetStream<Update>();
      stream.AddDelayed(1, () => flag = true);
      stream.AddOnce(async () => {
        await StreamTask.WaitWhile(() => !flag);
        tcs.SetResult(true);
      });
      Assert.IsTrue(await tcs.Task);
    }

    [Test, StreamTasks]
    public async Task WhenAllTest() {
      var tcs = new TaskCompletionSource<bool>();
      using var cts = new StreamTokenSource();
      int firstDelay = Random.Range(100, 1000);
      int secondDelay = Random.Range(100, 1000);
      int thirdDelay = Random.Range(100, 1000);
      Debug.Log($"First delay: {firstDelay}");
      Debug.Log($"Second delay: {secondDelay}");
      Debug.Log($"Third delay: {thirdDelay}");

      SetFailureAfterTime(2, tcs, cts.Token);
      UnityPlayerLoop.GetStream<Update>().AddOnce(async () => {
        await StreamTask.WhenAll(StreamTask.Delay(firstDelay), StreamTask.Delay(secondDelay), StreamTask.Delay(thirdDelay));
        tcs.SetResult(true);
      });
      Assert.IsTrue(await tcs.Task);
    }

    [Test, StreamTasks]
    public async Task WhenAnyTest() {
      var tcs = new TaskCompletionSource<bool>();
      using var cts = new StreamTokenSource();
      int firstDelay = Random.Range(100, 1000);
      int secondDelay = Random.Range(100, 1000);
      int thirdDelay = Random.Range(100, 1000);
      Debug.Log($"First delay: {firstDelay}");
      Debug.Log($"Second delay: {secondDelay}");
      Debug.Log($"Third delay: {thirdDelay}");

      SetFailureAfterTime(2, tcs, cts.Token);
      UnityPlayerLoop.GetStream<Update>().AddOnce(async () => {
        await StreamTask.WhenAny(StreamTask.Delay(firstDelay), StreamTask.Delay(secondDelay), StreamTask.Delay(thirdDelay));
        tcs.SetResult(true);
      });

      Assert.IsTrue(await tcs.Task);
    }

    private void SetFailureAfterTime(float time, TaskCompletionSource<bool> tcs, StreamToken token) {
      UnityPlayerLoop.GetStream<Update>().AddDelayed(time, () => tcs.SetResult(false), token);
    }

  }

}