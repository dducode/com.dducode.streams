using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using JetBrains.Annotations;

namespace StreamsForUnity.StreamStateMachine {

  public class StateMachine<TBaseSystem> : IStateMachine {

    public State CurrentState { get; private set; }

    private readonly Dictionary<Type, State> _states;
    private CancellationTokenSource _stateCancelling = new();
    private bool _entering;

    public StateMachine(CancellationToken disposeToken, [NotNull] params State[] states) {
      if (states == null)
        throw new ArgumentNullException(nameof(states));
      if (states.Length == 0)
        throw new ArgumentException("Value cannot be an empty collection", nameof(states));

      _states = states.ToDictionary(keySelector: state => state.GetType(), elementSelector: state => state);
      foreach (State state in states) 
        state.Initialize<TBaseSystem>(this, disposeToken);
    }

    public void SetState<TState>() where TState : State {
      Type stateType = typeof(TState);
      if (!_states.TryGetValue(stateType, out State state))
        throw new InvalidOperationException($"State machine doesn't contain state with type {stateType.Name}");

      ExitFromCurrentState();
      EnterToState(state);
    }

    private void ExitFromCurrentState() {
      _stateCancelling.Cancel();
      CurrentState?.Exit();
    }

    private void EnterToState(State state) {
      CurrentState = state;
      _stateCancelling = new CancellationTokenSource();
      state.Enter(_stateCancelling.Token);
    }

  }

}