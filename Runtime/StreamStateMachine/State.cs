using StreamsForUnity.Internal;
using StreamsForUnity.StreamRunners;

namespace StreamsForUnity.StreamStateMachine {

  public abstract class State {

    protected IStateMachine StateMachine { get; private set; }
    protected ExecutionStream Stream => _runner.Stream;

    private StreamTokenSource _lockHandle = new();
    private IStreamRunner _runner;

    internal void Initialize<TBaseSystem>(IStateMachine stateMachine, StreamToken disposeToken) {
      StateMachine = stateMachine;
      _runner = new StreamRunner<TBaseSystem>(disposeToken, NamesUtility.CreateProfilerSampleName(GetType()));
      _runner.Stream.Lock(_lockHandle.Token);
      OnInitialize();
    }

    internal void Enter(StreamToken subscriptionToken) {
      _lockHandle.Release();
      _lockHandle = null;
      OnEnter(subscriptionToken);
    }

    internal void Exit() {
      _lockHandle = new StreamTokenSource();
      _runner.Stream.Lock(_lockHandle.Token);
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