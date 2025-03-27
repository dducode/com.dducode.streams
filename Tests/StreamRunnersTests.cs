using System.Threading.Tasks;
using NUnit.Framework;
using Streams.StreamHolders;
using UnityEngine.PlayerLoop;

namespace Streams.Tests {

  public class StreamRunnersTests {

    [Test]
    public async Task MonoStreamRunnerTest() {
      var tcs = new TaskCompletionSource<bool>();
      var factory = new GameObjectStreamsHolderFactory();
      GameObjectStreamsHolder holder = factory.Create();
      holder.GetStream<Update>().AddOnce(() => tcs.SetResult(true));
      Assert.IsTrue(await tcs.Task);
    }

  }

}