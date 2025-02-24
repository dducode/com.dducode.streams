using UnityEngine;

namespace StreamsForUnity {

  public abstract class UpdatableBehaviour : MonoBehaviour {

    [field: SerializeField] public bool RunOnBackgroundThread { get; private set; }
    protected internal abstract void UpdateFunction(float deltaTime);

  }

}