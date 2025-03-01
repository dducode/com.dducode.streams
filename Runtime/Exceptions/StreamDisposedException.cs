using System;

namespace StreamsForUnity.Exceptions {

  public class StreamDisposedException : ObjectDisposedException {

    public StreamDisposedException(string objectName) : base(objectName, "The stream has been disposed") {
    }

  }

}