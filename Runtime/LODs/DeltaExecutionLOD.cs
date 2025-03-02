using StreamsForUnity.StreamActions;
using UnityEngine;

namespace StreamsForUnity.LODs {

  [AddComponentMenu("Streams/LODs/Delta Execution LOD")]
  [DisallowMultipleComponent]
  public sealed class DeltaExecutionLOD : ExecutionLOD<float> {

    protected override void SetValue(PersistentStreamAction action, float value) {
      action.SetDelta(value);
    }

  }

}