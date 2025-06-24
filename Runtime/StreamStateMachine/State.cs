using System.Threading;
using Streams.Internal;

namespace Streams.StreamStateMachine {

  public abstract class State {

    public ExecutionStream Stream => _stream;
    protected IStateMachine StateMachine { get; private set; }

    private ManagedExecutionStream _stream;
    private CancellationTokenSource _lockHandle = new();

    protected virtual void OnInitialize() {
    }

    protected virtual void OnEnter(CancellationToken subscriptionToken) {
    }

    protected virtual void OnExit() {
    }

    internal void Initialize<TSystem>(IStateMachine stateMachine, CancellationToken disposeToken) {
      StateMachine = stateMachine;
      _stream = new ManagedExecutionStream(UnityPlayerLoop.GetStream<TSystem>(), NamesUtility.CreateProfilerSampleName(GetType()));
      disposeToken.Register(_stream.Dispose);
      _stream.Lock(_lockHandle.Token);
      OnInitialize();
    }

    internal void Enter(CancellationToken subscriptionToken) {
      _lockHandle.Cancel();
      _lockHandle = null;
      OnEnter(subscriptionToken);
    }

    internal void Exit() {
      _lockHandle = new CancellationTokenSource();
      _stream.Lock(_lockHandle.Token);
      OnExit();
    }

  }

}