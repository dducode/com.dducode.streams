using System.Threading.Tasks;
using NUnit.Framework;
using StreamsForUnity.Tests.Attributes;
using UnityEngine;
using UnityEngine.PlayerLoop;
using UnityEngine.SceneManagement;

namespace StreamsForUnity.Tests {

  public class StreamsTests {

    [Test, Common]
    public async Task DeltaTest() {
      var tcs = new TaskCompletionSource<bool>();
      var cts = new StreamTokenSource();
      Streams.Get<Update>().Add(delta => {
        tcs.SetResult(Mathf.Approximately(delta, Time.deltaTime));
        cts.Release();
      }, cts.Token);

      Assert.IsTrue(await tcs.Task);
    }

    [Test, Common]
    public async Task FixedDeltaTest() {
      var tcs = new TaskCompletionSource<bool>();
      var cts = new StreamTokenSource();
      Streams.Get<FixedUpdate>().Add(delta => {
        tcs.SetResult(Mathf.Approximately(delta, Time.fixedDeltaTime));
        cts.Release();
      }, cts.Token);

      Assert.IsTrue(await tcs.Task);
    }

    [Test, Common]
    public async Task TemporaryUpdateTest() {
      var tcs = new TaskCompletionSource<bool>();

      Streams.Get<Update>().AddTemporary(2, delta => {
        Debug.Log(delta);
      }).SetDelta(0.5f).OnDispose(() => tcs.SetResult(true));

      Assert.IsTrue(await tcs.Task);
    }

    [Test, Common]
    public async Task FixedUpdateTest() {
      var tcs = new TaskCompletionSource<bool>();

      Streams.Get<FixedUpdate>().AddTemporary(0.2f, delta => {
        Debug.Log(delta);
      }).SetDelta(0.002f).OnDispose(() => tcs.SetResult(true));

      Assert.IsTrue(await tcs.Task);
    }

    [Test, Common]
    public async Task ConditionalTest() {
      var tcs = new TaskCompletionSource<bool>();
      var disposeHandle = new StreamTokenSource();

      Streams.Get<Update>()
        .AddConditional(() => true, delta => Debug.Log(delta), disposeHandle.Token)
        .SetDelta(0.1f)
        .OnDispose(() => tcs.SetResult(true));

      Streams.Get<Update>().AddTimer(2, () => disposeHandle.Release());
      Assert.IsTrue(await tcs.Task);
    }

    [Test, Common]
    public async Task LockStreamTest() {
      var tcs = new TaskCompletionSource<bool>();
      var lockHandle = new StreamTokenSource();
      var disposeHandle = new StreamTokenSource();

      ExecutionStream baseStream = Streams.Get<FixedUpdate>();
      var stream = new ManagedExecutionStream(baseStream);
      disposeHandle.Register(stream.Dispose);

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
      lockHandle.Register(() => {
        firstLockHandle.Release();
        secondLockHandle.Release();
        thirdLockHandle.Release();
      });

      var disposeHandle = new StreamTokenSource();

      ExecutionStream baseStream = Streams.Get<FixedUpdate>();
      var stream = new ManagedExecutionStream(baseStream);
      disposeHandle.Register(stream.Dispose);

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
      ExecutionStream stream = Streams.Get<Update>();
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
      ExecutionStream stream = Streams.Get<Update>();
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

  }

}