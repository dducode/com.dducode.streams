using UnityEngine;

namespace StreamsForUnity.LODs {

  [AddComponentMenu("Streams/LODs/Delta Execution LOD")]
  [DisallowMultipleComponent]
  public sealed class DeltaExecutionLOD : ExecutionLOD<float> {

    protected override void SetValue(StreamAction action, float value) {
      action.SetDelta(value);
    }

  }

}