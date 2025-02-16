using StreamsForUnity.StreamHolders.MonoStreamHolders;
using UnityEngine;

namespace StreamsForUnity.Tests {

  public class PriorityCheck : MonoBehaviour {

    public void CheckStreamPriority(UpdateStreamHolder holder) {
      Debug.Log(holder.name);
    }

  }

}