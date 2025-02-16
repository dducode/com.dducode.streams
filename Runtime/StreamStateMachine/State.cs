using StreamsForUnity.Internal;
using StreamsForUnity.StreamHolders;

namespace StreamsForUnity.StreamStateMachine {

  public abstract class State {

    protected IStateMachine StateMachine { get; private set; }
    protected ExecutionStream Stream => _holder.Stream;

    private StreamTokenSource _lockHandle = new();
    private IStreamHolder _holder;

    internal void Initialize<TBaseSystem>(IStateMachine stateMachine, StreamToken disposeToken) {
      StateMachine = stateMachine;
      _holder = new StreamHolder<TBaseSystem>(disposeToken, NamesUtility.CreateProfilerSampleName(GetType()));
      _holder.Stream.Lock(_lockHandle.Token);
      OnInitialize();
    }

    internal void Enter(StreamToken subscriptionToken) {
      _lockHandle.Release();
      _lockHandle = null;
      OnEnter(subscriptionToken);
    }

    internal void Exit() {
      _lockHandle = new StreamTokenSource();
      _holder.Stream.Lock(_lockHandle.Token);
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