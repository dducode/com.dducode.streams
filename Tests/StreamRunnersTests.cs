using System.Threading.Tasks;
using NUnit.Framework;
using StreamsForUnity.StreamHolders;

namespace StreamsForUnity.Tests {

  public class StreamRunnersTests {

    [Test]
    public async Task MonoStreamRunnerTest() {
      var tcs = new TaskCompletionSource<bool>();
      var factory = new MonoStreamHolderFactory();
      var runner = factory.Create<UpdateStreamHolder>();
      runner.Stream.AddOnce(() => tcs.SetResult(true));
      Assert.IsTrue(await tcs.Task);
    }

  }

}