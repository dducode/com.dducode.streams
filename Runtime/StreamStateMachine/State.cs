using System.Threading;
using StreamsForUnity.Internal;
using StreamsForUnity.StreamRunners;

namespace StreamsForUnity.StreamStateMachine {

  public abstract class State {

    protected IStateMachine StateMachine { get; private set; }
    protected ExecutionStream Stream => _runner.Stream;

    private CancellationTokenSource _lockHandle = new();
    private IStreamRunner _runner;

    internal void Initialize<TBaseSystem>(IStateMachine stateMachine, CancellationToken disposeToken) {
      StateMachine = stateMachine;
      _runner = new StreamRunner<TBaseSystem>(disposeToken, NamesUtility.CreateProfilerSampleName(GetType()));
      _runner.Stream.Lock(_lockHandle.Token);
      OnInitialize();
    }

    internal void Enter(CancellationToken subscriptionToken) {
      _lockHandle.Cancel();
      _lockHandle = null;
      OnEnter(subscriptionToken);
    }

    internal void Exit() {
      _lockHandle = new CancellationTokenSource();
      _runner.Stream.Lock(_lockHandle.Token);
      OnExit();
    }

    protected virtual void OnInitialize() {
    }

    protected virtual void OnEnter(CancellationToken subscriptionToken) {
    }

    protected virtual void OnExit() {
    }

  }

}