using System.Threading.Tasks;
using NUnit.Framework;
using Streams.StreamContexts;
using UnityEngine.PlayerLoop;

namespace Streams.Tests {

  public class StreamRunnersTests {

    [Test]
    public async Task MonoStreamRunnerTest() {
      var tcs = new TaskCompletionSource<bool>();
      var factory = new GameObjectStreamsContextFactory();
      GameObjectExecutionContext context = factory.Create();
      context.GetStream<Update>().AddOnce(() => tcs.SetResult(true));
      Assert.IsTrue(await tcs.Task);
    }

  }

}