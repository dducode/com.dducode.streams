namespace StreamsForUnity {

  public class StreamBuilder {

    private readonly ExecutionStream _baseStream;
    private string _name = nameof(ManagedExecutionStream);
    private uint _priority = uint.MaxValue;
    private StreamUnlockMode _unlockMode = StreamUnlockMode.WhenAll;

    public static StreamBuilder New(ExecutionStream baseStream) {
      return new StreamBuilder(baseStream);
    }

    private StreamBuilder(ExecutionStream baseStream) {
      _baseStream = baseStream;
    }

    public StreamBuilder WithName(string name) {
      _name = name;
      return this;
    }

    public StreamBuilder SetPriority(uint priority) {
      _priority = priority;
      return this;
    }

    public StreamBuilder SetUnlockMode(StreamUnlockMode unlockMode) {
      _unlockMode = unlockMode;
      return this;
    }

    public ManagedExecutionStream Build() {
      return new ManagedExecutionStream(_baseStream, _name, _priority, _unlockMode);
    }

  }

}