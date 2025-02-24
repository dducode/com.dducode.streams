namespace StreamsForUnity.LODs {

  internal class LODItem {

    public StreamAction Action { get; }
    public int levelIndex;

    public LODItem(StreamAction action) {
      Action = action;
    }

  }

}