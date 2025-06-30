using System.Collections.Generic;
using Streams.Exceptions;
using Streams.StreamActions;

namespace Streams.Internal {

  internal class StreamActionComparer : IComparer<StreamActionBase> {

    public int Compare(StreamActionBase first, StreamActionBase second) {
      if (first == null || second == null)
        throw new StreamsException("Internal error was occurred while comparing - actions cannot be null");

      if (first is IConfigurable firstConfigurable && second is IConfigurable secondConfigurable) {
        int result = firstConfigurable.Priority.CompareTo(secondConfigurable.Priority);
        if (result != 0)
          return result;
      }
      else if (first is IConfigurable) {
        return -1;
      }
      else if (second is IConfigurable) {
        return 1;
      }

      return first.Id.CompareTo(second.Id);
    }

  }

}