using StreamsForUnity.StreamActions;
using UnityEngine;

namespace StreamsForUnity.StreamHolders {

  /// <summary>
  /// MonoBehaviour component that contains and controls the associated <see cref="Stream"/>.
  /// It exists as a game object and controls the stream's <see cref="Priority"/> using the transform sibling index.
  /// These values are closely related - when the sibling index changes, the priority changes, and vice versa
  /// </summary>
  public abstract class StreamHolderBase : MonoBehaviour, IStreamHolder, IConfigurable<StreamHolderBase> {

    [SerializeField] protected MonoBehaviour[] connectedBehaviours;

    public abstract ExecutionStream Stream { get; }
    
    /// <summary>
    /// The priority of the associated stream
    /// </summary>
    public abstract uint Priority { get; set; }

    public abstract StreamHolderBase SetDelta(float value);
    public abstract StreamHolderBase ResetDelta();
    public abstract StreamHolderBase SetTickRate(uint value);

    public override string ToString() {
      return gameObject.name;
    }

  }

}