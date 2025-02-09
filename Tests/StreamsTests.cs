using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.PlayerLoop;
using UnityEngine.SceneManagement;

namespace StreamsForUnity.Tests {

  public class StreamsTests {

    [Test]
    public async Task DeltaTest() {
      var tcs = new TaskCompletionSource<bool>();
      Streams.Get<Update.ScriptRunBehaviourUpdate>().AddOnce(delta => {
        tcs.SetResult(Mathf.Approximately(delta, Time.deltaTime));
      });

      Assert.IsTrue(await tcs.Task);
    }

    [Test]
    public async Task FixedDeltaTest() {
      var tcs = new TaskCompletionSource<bool>();
      Streams.Get<FixedUpdate.ScriptRunBehaviourFixedUpdate>().AddOnce(delta => {
        tcs.SetResult(Mathf.Approximately(delta, Time.fixedDeltaTime));
      });

      Assert.IsTrue(await tcs.Task);
    }

    [Test]
    public async Task TemporaryUpdateTest() {
      var tcs = new TaskCompletionSource<bool>();

      Streams.Get<Update.ScriptRunBehaviourUpdate>().AddTemporary(2, delta => {
        Debug.Log(delta);
      }).SetDelta(0.5f).OnDispose(() => tcs.SetResult(true));

      Assert.IsTrue(await tcs.Task);
    }

    [Test]
    public async Task FixedUpdateTest() {
      var tcs = new TaskCompletionSource<bool>();

      Streams.Get<FixedUpdate.ScriptRunBehaviourFixedUpdate>().AddTemporary(0.2f, delta => {
        Debug.Log(delta);
      }).SetDelta(0.002f).OnDispose(() => tcs.SetResult(true));

      Assert.IsTrue(await tcs.Task);
    }

    [Test]
    public async Task ConditionalTest() {
      var tcs = new TaskCompletionSource<bool>();
      var disposeHandle = new CancellationTokenSource();

      Streams.Get<Update.ScriptRunBehaviourUpdate>()
        .AddConditional(() => true, delta => Debug.Log(delta), disposeHandle.Token)
        .SetDelta(0.1f)
        .OnDispose(() => tcs.SetResult(true));

      Streams.Get<Update.ScriptRunBehaviourUpdate>().AddTimer(2, () => disposeHandle.Cancel());
      Assert.IsTrue(await tcs.Task);
    }

    [Test]
    public async Task LockStreamTest() {
      var tcs = new TaskCompletionSource<bool>();
      var lockHandle = new CancellationTokenSource();

      Streams.Get<Update.ScriptRunBehaviourUpdate>()
        .Add(deltaTime => Debug.Log(deltaTime))
        .SetDelta(0.1f);

      ExecutionStream stream = Streams.Get<FixedUpdate.ScriptRunBehaviourFixedUpdate>();
      stream.AddTimer(2, () => Streams.Get<Update.ScriptRunBehaviourUpdate>().Lock(lockHandle.Token));
      stream.AddTimer(4, () => lockHandle.Cancel());
      stream.AddTimer(6, () => tcs.SetResult(true));

      Assert.IsTrue(await tcs.Task);
    }

    [Test]
    public async Task SceneStreamTest() {
      var tcs = new TaskCompletionSource<bool>();
      Scene scene = SceneManager.CreateScene("Test");
      scene.GetStream<Update.ScriptRunBehaviourUpdate>().AddOnce(_ => tcs.SetResult(true));
      Assert.IsTrue(await tcs.Task);
    }

    [Test]
    public async Task ActionsChainTest() {
      var tcs = new TaskCompletionSource<bool>();
      ExecutionStream stream = Streams.Get<Update.ScriptRunBehaviourUpdate>();
      stream.AddOnce(_ => {
        Debug.Log(1);
        stream.AddOnce(_ => {
          Debug.Log(2);
          stream.AddOnce(_ => {
            Debug.Log(3);
            tcs.SetResult(true);
          });
        });
      });
      Assert.IsTrue(await tcs.Task);
    }

    [Test]
    public async Task PriorityActionsTest() {
      var tcs = new TaskCompletionSource<bool>();
      ExecutionStream stream = Streams.Get<Update.ScriptRunBehaviourUpdate>();
      stream.AddOnce(_ => Debug.Log(5), priority: 5).OnDispose(() => tcs.SetResult(true));
      stream.AddOnce(_ => Debug.Log(1), priority: 1);
      stream.AddOnce(_ => Debug.Log(4), priority: 4);
      stream.AddOnce(_ => Debug.Log(2), priority: 2);
      stream.AddOnce(_ => Debug.Log(3), priority: 3);
      Assert.IsTrue(await tcs.Task);
    }

  }

}