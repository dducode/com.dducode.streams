using StreamsForUnity.Internal;
using StreamsForUnity.StreamHolders;

namespace StreamsForUnity.StreamStateMachine {

  public abstract class State : IStreamHolder {

    public ExecutionStream Stream => _stream;
    protected IStateMachine StateMachine { get; private set; }

    private ManagedExecutionStream _stream;
    private StreamTokenSource _lockHandle = new();

    protected virtual void OnInitialize() {
    }

    protected virtual void OnEnter(StreamToken subscriptionToken) {
    }

    protected virtual void OnExit() {
    }

    internal void Initialize<TSystem>(IStateMachine stateMachine, StreamToken disposeToken) {
      StateMachine = stateMachine;
      _stream = new ManagedExecutionStream(Streams.Get<TSystem>(), NamesUtility.CreateProfilerSampleName(GetType()));
      disposeToken.Register(_stream.Dispose);
      _stream.Lock(_lockHandle.Token);
      OnInitialize();
    }

    internal void Enter(StreamToken subscriptionToken) {
      _lockHandle.Release();
      _lockHandle = null;
      OnEnter(subscriptionToken);
    }

    internal void Exit() {
      _lockHandle = new StreamTokenSource();
      _stream.Lock(_lockHandle.Token);
      OnExit();
    }

  }

}