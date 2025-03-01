using System.Collections.Generic;
using StreamsForUnity.Exceptions;

namespace StreamsForUnity.Internal {

  internal class StreamActionComparer : IComparer<StreamAction> {

    public int Compare(StreamAction first, StreamAction second) {
      if (first == null || second == null)
        throw new StreamsException("Internal error was occurred while comparing - actions cannot be null");
      int result = first.Priority.CompareTo(second.Priority);
      return result != 0 ? result : first.Id.CompareTo(second.Id);
    }

  }

}