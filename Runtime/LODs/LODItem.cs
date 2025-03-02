using StreamsForUnity.StreamActions;

namespace StreamsForUnity.LODs {

  internal class LODItem {

    public PersistentStreamAction Action { get; }
    public int levelIndex;

    public LODItem(PersistentStreamAction action) {
      Action = action;
    }

  }

}