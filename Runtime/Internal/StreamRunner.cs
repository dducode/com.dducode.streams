using UnityEngine;

namespace Streams.Internal {

  internal class StreamRunner {

    private readonly ExecutionStream _stream;
    private readonly bool _isFixedSystem;

    public StreamRunner(ExecutionStream stream, bool isFixedSystem) {
      _stream = stream;
      _isFixedSystem = isFixedSystem;
    }

    public void Run() {
      _stream.Update(_isFixedSystem ? Time.fixedDeltaTime : Time.deltaTime);
    }

  }

}