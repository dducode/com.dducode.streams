using System.Collections;
using System.Threading.Tasks;
using NUnit.Framework;
using Streams.Tests.Attributes;
using UnityEngine;
using UnityEngine.PlayerLoop;
using UnityEngine.SceneManagement;

namespace Streams.Tests {

  public class StreamsTests {

    [Test, Common]
    public async Task DeltaTest() {
      var tcs = new TaskCompletionSource<bool>();
      var cts = new StreamTokenSource();
      UnityPlayerLoop.GetStream<Update>().Add(delta => {
        tcs.SetResult(Mathf.Approximately(delta, Time.deltaTime));
        cts.Release();
      }, cts.Token);

      Assert.IsTrue(await tcs.Task);
    }

    [Test, Common]
    public async Task FixedDeltaTest() {
      var tcs = new TaskCompletionSource<bool>();
      var cts = new StreamTokenSource();
      UnityPlayerLoop.GetStream<FixedUpdate>().Add(delta => {
        tcs.SetResult(Mathf.Approximately(delta, Time.fixedDeltaTime));
        cts.Release();
      }, cts.Token);

      Assert.IsTrue(await tcs.Task);
    }

    [Test, Common]
    public async Task TemporaryUpdateTest() {
      var tcs = new TaskCompletionSource<bool>();

      UnityPlayerLoop.GetStream<Update>().Add(2, delta => {
        Debug.Log(delta);
      }).SetDelta(0.5f).OnComplete(() => tcs.SetResult(true));

      Assert.IsTrue(await tcs.Task);
    }

    [Test, Common]
    public async Task FixedUpdateTest() {
      var tcs = new TaskCompletionSource<bool>();

      UnityPlayerLoop.GetStream<FixedUpdate>().Add(0.2f, delta => {
        Debug.Log(delta);
      }).SetDelta(0.002f).OnComplete(() => tcs.SetResult(true));

      Assert.IsTrue(await tcs.Task);
    }

    [Test, Common]
    public async Task ConditionalTest() {
      var tcs = new TaskCompletionSource<bool>();
      var completionHandle = new StreamTokenSource();

      UnityPlayerLoop.GetStream<Update>()
        .Add(() => !completionHandle.Released, delta => Debug.Log(delta))
        .SetDelta(0.1f); // TODO: fix test

      UnityPlayerLoop.GetStream<Update>().AddTimer(2, () => completionHandle.Release());
      Assert.IsTrue(await tcs.Task);
    }

    [Test, Common]
    public async Task LockStreamTest() {
      var tcs = new TaskCompletionSource<bool>();
      var lockHandle = new StreamTokenSource();
      var disposeHandle = new StreamTokenSource();

      ExecutionStream baseStream = UnityPlayerLoop.GetStream<FixedUpdate>();
      var stream = new ManagedExecutionStream(baseStream);
      disposeHandle.Token.Register(stream.Dispose);

      stream.Add(deltaTime => Debug.Log(deltaTime))
        .SetDelta(0.1f);

      baseStream.AddTimer(2, () => stream.Lock(lockHandle.Token));
      baseStream.AddTimer(4, () => lockHandle.Release());
      baseStream.AddTimer(6, () => tcs.SetResult(true));

      Assert.IsTrue(await tcs.Task);
      disposeHandle.Release();
    }

    [Test, Common]
    public async Task ManyLockersTest() {
      var tcs = new TaskCompletionSource<bool>();
      var lockHandle = new StreamTokenSource();
      var firstLockHandle = new StreamTokenSource();
      var secondLockHandle = new StreamTokenSource();
      var thirdLockHandle = new StreamTokenSource();
      lockHandle.Token.Register(() => {
        firstLockHandle.Release();
        secondLockHandle.Release();
        thirdLockHandle.Release();
      });

      var disposeHandle = new StreamTokenSource();

      ExecutionStream baseStream = UnityPlayerLoop.GetStream<FixedUpdate>();
      var stream = new ManagedExecutionStream(baseStream);
      disposeHandle.Token.Register(stream.Dispose);

      stream.Add(deltaTime => Debug.Log(deltaTime))
        .SetDelta(0.1f);

      baseStream.AddTimer(2, () => {
        stream.Lock(firstLockHandle.Token);
        stream.Lock(secondLockHandle.Token);
        stream.Lock(thirdLockHandle.Token);
      });
      baseStream.AddTimer(4, () => lockHandle.Release());
      baseStream.AddTimer(6, () => tcs.SetResult(true));

      Assert.IsTrue(await tcs.Task);
      disposeHandle.Release();
    }

    [Test, Common]
    public async Task SceneStreamTest() {
      var tcs = new TaskCompletionSource<bool>();
      Scene scene = SceneManager.CreateScene("Test");
      scene.GetStream<Update>().AddOnce(() => tcs.SetResult(true));
      Assert.IsTrue(await tcs.Task);
    }

    [Test, Common]
    public async Task ActionsChainTest() {
      var tcs = new TaskCompletionSource<bool>();
      ExecutionStream stream = UnityPlayerLoop.GetStream<Update>();
      stream.AddOnce(() => {
        Debug.Log(1);
        stream.AddOnce(() => {
          Debug.Log(2);
          stream.AddOnce(() => {
            Debug.Log(3);
            tcs.SetResult(true);
          });
        });
      });
      Assert.IsTrue(await tcs.Task);
    }

    [Test, Common]
    public async Task PriorityActionsTest() {
      var tcs = new TaskCompletionSource<bool>();
      ExecutionStream stream = UnityPlayerLoop.GetStream<Update>();
      stream.AddOnce(() => {
        Debug.Log(5);
        tcs.SetResult(true);
      }, priority: 5);
      stream.AddOnce(() => Debug.Log(1), priority: 1);
      stream.AddOnce(() => Debug.Log(4), priority: 4);
      stream.AddOnce(() => Debug.Log(2), priority: 2);
      stream.AddOnce(() => Debug.Log(3), priority: 3);
      Assert.IsTrue(await tcs.Task);
    }

    [Test, Common]
    public async Task Test() {
      var tcs = new TaskCompletionSource<bool>();
      UnityPlayerLoop.GetStream<Update>().Add(Coroutine);
      UnityPlayerLoop.GetStream<Update>().AddTimer(0.1f, () => tcs.SetResult(true));
      Assert.IsTrue(await tcs.Task);
    }

    private IEnumerator Coroutine() {
      Debug.Log(1);
      yield return null;
      Debug.Log(2);
      yield return null;
      Debug.Log(3);
    }

  }

}