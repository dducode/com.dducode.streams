using UnityEngine;

namespace StreamsForUnity.LODs {

  [AddComponentMenu("Streams/LODs/Tick Rate Execution LOD")]
  [DisallowMultipleComponent]
  public sealed class TickRateExecutionLOD : ExecutionLOD<uint> {

    protected override void SetValue(StreamAction action, uint value) {
      action.SetTickRate(value);
    }

  }

}