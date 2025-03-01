using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;

namespace StreamsForUnity.StreamStateMachine {

  /// <summary>
  /// The finite state machine that allows you to control multiple streams as states
  /// </summary>
  public class StateMachine<TSystem> : IStateMachine {

    public State CurrentState { get; private set; }

    private readonly Dictionary<Type, State> _states;
    private readonly StreamTokenSource _disposeHandle;
    private StreamTokenSource _stateCancelling = new();

    public StateMachine([NotNull] params State[] states) {
      if (states == null)
        throw new ArgumentNullException(nameof(states));
      if (states.Length == 0)
        throw new ArgumentException("Value cannot be an empty collection", nameof(states));

      _states = states.ToDictionary(keySelector: state => state.GetType(), elementSelector: state => state);
      _disposeHandle = new StreamTokenSource();
      foreach (State state in states)
        state.Initialize<TSystem>(this, _disposeHandle.Token);
      EnterToState(states.First());
    }

    public void SetState<TState>() where TState : State {
      Type stateType = typeof(TState);
      if (!_states.TryGetValue(stateType, out State state))
        throw new InvalidOperationException($"State machine doesn't contain state with type {stateType.Name}");

      ExitFromCurrentState();
      EnterToState(state);
    }

    public void Dispose() {
      _stateCancelling.Dispose();
      _disposeHandle.Dispose();
    }

    private void ExitFromCurrentState() {
      _stateCancelling.Release();
      CurrentState.Exit();
    }

    private void EnterToState(State state) {
      CurrentState = state;
      _stateCancelling = new StreamTokenSource();
      state.Enter(_stateCancelling.Token);
    }

  }

}