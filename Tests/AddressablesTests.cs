#if STREAMS_ADDRESSABLES_INTEGRATION
using System.Threading.Tasks;
using NUnit.Framework;
using Streams.Extensions;
using Streams.Tests.Attributes;
using UnityEngine;
using UnityEngine.PlayerLoop;

namespace Streams.Tests {

  public class AddressablesTests {

    private const string Key = "Assets/com.dducode.streams/Tests/Addressables/_Resources/text.txt";

    [Test, Common]
    public async Task LoadAssetAsyncTest() {
      var tcs = new TaskCompletionSource<bool>();

      UnityPlayerLoop.GetStream<Update>().AddOnce(async () => {
        ExecutionStream runningStream = ExecutionStream.RunningStream;
        var text = await UnityEngine.AddressableAssets.Addressables.LoadAssetAsync<TextAsset>(Key);
        Debug.Log(text.text);
        tcs.SetResult(ExecutionStream.RunningStream == runningStream);
      });

      Assert.IsTrue(await tcs.Task);
    }

  }

}
#endif