using System;

namespace Streams.Exceptions {

  public class StreamDisposedException : ObjectDisposedException {

    public StreamDisposedException(string objectName) : base(objectName, "The stream has been disposed") {
    }

  }

}