using System.Threading.Tasks;
using NUnit.Framework;
using Streams.Extensions;
using Streams.StreamActions;
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
      UnityPlayerLoop.GetStream<Update>().Add(deltaTime => {
        tcs.SetResult(Mathf.Approximately(deltaTime, Time.deltaTime));
        cts.Release();
      }, cts.Token);

      Assert.IsTrue(await tcs.Task);
    }

    [Test, Common]
    public async Task FixedDeltaTest() {
      var tcs = new TaskCompletionSource<bool>();
      var cts = new StreamTokenSource();
      UnityPlayerLoop.GetStream<FixedUpdate>().Add(deltaTime => {
        tcs.SetResult(Mathf.Approximately(deltaTime, Time.fixedDeltaTime));
        cts.Release();
      }, cts.Token);

      Assert.IsTrue(await tcs.Task);
    }

    [Test, Common]
    public async Task TemporaryUpdateTest() {
      var tcs = new TaskCompletionSource<bool>();
      var cts = new StreamTokenSource();

      UnityPlayerLoop.GetStream<Update>().Add(deltaTime => {
        Debug.Log(deltaTime);
      }, cts.Token).SetDelta(0.5f);

      UnityPlayerLoop.GetStream<Update>().AddDelayed(2, () => {
        cts.Release();
        tcs.SetResult(true);
      });

      Assert.IsTrue(await tcs.Task);
    }

    [Test, Common]
    public async Task FixedUpdateTest() {
      var tcs = new TaskCompletionSource<bool>();

      ExecutionStream stream = UnityPlayerLoop.GetStream<FixedUpdate>();
      stream.Add(deltaTime => Debug.Log(deltaTime), stream.CreateTokenTerminatedAfter(0.2f)).SetDelta(0.002f);
      stream.AddDelayed(0.2f, () => tcs.SetResult(true));

      Assert.IsTrue(await tcs.Task);
    }

    [Test, Common]
    public async Task ConditionalSleepTest() {
      var tcs = new TaskCompletionSource<bool>();

      ExecutionStream stream = UnityPlayerLoop.GetStream<Update>();
      IConfigurable action = stream.Add(deltaTime => Debug.Log(deltaTime)).SetDelta(0.1f);

      stream.AddDelayed(2, () => {
        StreamToken lockToken = stream.CreateTokenTerminatedAfter(2);
        action.Lock(lockToken);
        lockToken.Register(() => stream.AddDelayed(2, () => tcs.SetResult(true)));
      });

      Assert.IsTrue(await tcs.Task);
    }

    [Test, Common]
    public async Task LockStreamTest() {
      var tcs = new TaskCompletionSource<bool>();
      var disposeHandle = new StreamTokenSource();

      ExecutionStream baseStream = UnityPlayerLoop.GetStream<FixedUpdate>();
      var stream = new ManagedExecutionStream(baseStream);
      disposeHandle.Token.Register(stream.Dispose);

      stream.Add(deltaTime => Debug.Log(deltaTime)).SetDelta(0.1f);

      baseStream.AddDelayed(2, () => {
        StreamToken lockHandle = baseStream.CreateTokenTerminatedAfter(2);
        stream.Lock(lockHandle);
        lockHandle.Register(() => baseStream.AddDelayed(2, () => tcs.SetResult(true)));
      });

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

      baseStream.AddDelayed(2, () => {
        stream.Lock(firstLockHandle.Token);
        stream.Lock(secondLockHandle.Token);
        stream.Lock(thirdLockHandle.Token);
      });
      baseStream.AddDelayed(4, () => lockHandle.Release());
      baseStream.AddDelayed(6, () => tcs.SetResult(true));

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