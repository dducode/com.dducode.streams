using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using JetBrains.Annotations;

namespace Streams.StreamStateMachine {

  /// <summary>
  /// The finite state machine that allows you to control multiple streams as states
  /// </summary>
  public class StateMachine<TSystem> : IStateMachine {

    public State CurrentState { get; private set; }

    private readonly Dictionary<Type, State> _states;
    private readonly CancellationTokenSource _disposeHandle;
    private CancellationTokenSource _stateCancelling = new();

    public StateMachine([NotNull] params State[] states) {
      if (states == null)
        throw new ArgumentNullException(nameof(states));
      if (states.Length == 0)
        throw new ArgumentException("Value cannot be an empty collection", nameof(states));

      _states = states.ToDictionary(keySelector: state => state.GetType(), elementSelector: state => state);
      _disposeHandle = new CancellationTokenSource();
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
      _stateCancelling.Cancel();
      CurrentState.Exit();
    }

    private void EnterToState(State state) {
      CurrentState = state;
      _stateCancelling = new CancellationTokenSource();
      state.Enter(_stateCancelling.Token);
    }

  }

}