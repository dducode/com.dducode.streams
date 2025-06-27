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
      UnityPlayerLoop.GetStream<Update>().Add(self => {
        tcs.SetResult(Mathf.Approximately(self.DeltaTime, Time.deltaTime));
        cts.Release();
      }, cts.Token);

      Assert.IsTrue(await tcs.Task);
    }

    [Test, Common]
    public async Task FixedDeltaTest() {
      var tcs = new TaskCompletionSource<bool>();
      var cts = new StreamTokenSource();
      UnityPlayerLoop.GetStream<FixedUpdate>().Add(self => {
        tcs.SetResult(Mathf.Approximately(self.DeltaTime, Time.fixedDeltaTime));
        cts.Release();
      }, cts.Token);

      Assert.IsTrue(await tcs.Task);
    }

    [Test, Common]
    public async Task TemporaryUpdateTest() {
      var tcs = new TaskCompletionSource<bool>();

      UnityPlayerLoop.GetStream<Update>().Add(2, self => {
        Debug.Log(self.DeltaTime);
      }).SetDelta(0.5f).OnComplete(() => tcs.SetResult(true));

      Assert.IsTrue(await tcs.Task);
    }

    [Test, Common]
    public async Task FixedUpdateTest() {
      var tcs = new TaskCompletionSource<bool>();

      UnityPlayerLoop.GetStream<FixedUpdate>().Add(0.2f, self => {
        Debug.Log(self.DeltaTime);
      }).SetDelta(0.002f).OnComplete(() => tcs.SetResult(true));

      Assert.IsTrue(await tcs.Task);
    }

    [Test, Common]
    public async Task ConditionalSleepTest() {
      var tcs = new TaskCompletionSource<bool>();
      var canRun = true;

      UnityPlayerLoop.GetStream<Update>()
        .Add(self => {
          if (!canRun)
            self.Sleep(() => canRun);
          Debug.Log(self.DeltaTime);
        })
        .SetDelta(0.1f);

      UnityPlayerLoop.GetStream<Update>().AddTimer(2, () => canRun = false);
      UnityPlayerLoop.GetStream<Update>().AddTimer(4, () => canRun = true);
      UnityPlayerLoop.GetStream<Update>().AddTimer(6, () => tcs.SetResult(true));
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

      stream.Add(self => Debug.Log(self.DeltaTime))
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

      stream.Add(self => Debug.Log(self.DeltaTime))
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

  }

}