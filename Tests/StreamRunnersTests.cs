using System.Threading.Tasks;
using NUnit.Framework;
using StreamsForUnity.StreamRunners.MonoStreamRunners;

namespace StreamsForUnity.Tests {

  public class StreamRunnersTests {

    [Test]
    public async Task MonoStreamRunnerTest() {
      var tcs = new TaskCompletionSource<bool>();
      var factory = new MonoStreamRunnerFactory();
      var runner = factory.Create<UpdateStreamRunner>();
      runner.Stream.AddOnce(_ => tcs.SetResult(true));
      Assert.IsTrue(await tcs.Task);
    }

  }

}