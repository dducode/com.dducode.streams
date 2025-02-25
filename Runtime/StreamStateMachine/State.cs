using StreamsForUnity.Internal;

namespace StreamsForUnity.StreamStateMachine {

  public abstract class State {

    protected IStateMachine StateMachine { get; private set; }
    protected ManagedExecutionStream Stream { get; private set; }

    private StreamTokenSource _lockHandle = new();

    internal void Initialize<TBaseSystem>(IStateMachine stateMachine, StreamToken disposeToken) {
      StateMachine = stateMachine;
      Stream = new ManagedExecutionStream(Streams.Get<TBaseSystem>(), NamesUtility.CreateProfilerSampleName(GetType()));
      disposeToken.Register(Stream.Dispose);
      Stream.Lock(_lockHandle.Token);
      OnInitialize();
    }

    internal void Enter(StreamToken subscriptionToken) {
      _lockHandle.Release();
      _lockHandle = null;
      OnEnter(subscriptionToken);
    }

    internal void Exit() {
      _lockHandle = new StreamTokenSource();
      Stream.Lock(_lockHandle.Token);
      OnExit();
    }

    protected virtual void OnInitialize() {
    }

    protected virtual void OnEnter(StreamToken subscriptionToken) {
    }

    protected virtual void OnExit() {
    }

  }

}