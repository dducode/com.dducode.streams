using System.Threading.Tasks;
using NUnit.Framework;
using StreamsForUnity.StreamHolders;
using UnityEngine.PlayerLoop;

namespace StreamsForUnity.Tests {

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