using StreamsForUnity.StreamRunners.MonoStreamRunners;
using UnityEngine;

namespace StreamsForUnity.Tests {

  public class PriorityCheck : MonoBehaviour {

    public void CheckStreamPriority(UpdateStreamRunner runner) {
      Debug.Log(runner.name);
    }

  }

}