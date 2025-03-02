using System;
using System.Diagnostics.CodeAnalysis;
using StreamsForUnity.Exceptions;
using StreamsForUnity.Internal;
using StreamsForUnity.StreamActions;
using StreamsForUnity.StreamTasks;
using UnityEngine.Profiling;
using Debug = UnityEngine.Debug;

namespace StreamsForUnity {

  /// <summary>
  /// <p> Base class for all streams in the system. Execution stream contains and executes any actions. </p>
  /// <p> Base streams executed on the Player Loop Systems, and they can be obtained from <see cref="Streams.Get{TSystem}"/> method.
  /// If you want to get the stream from scene, you can call <see cref="SceneStreams.GetStream{TBaseSystem}"/> </p>
  /// <code>
  /// Streams.Get&lt;Update&gt;().Add(deltaTime => {
  ///   // do something inside the base stream in the Update system
  /// });
  /// </code>
  /// <code>
  /// SceneManager.GetActiveScene().GetStream&lt;FixedUpdate&gt;().Add(deltaTime => {
  ///   // do something inside the active scene stream in the FixedUpdate system
  /// });
  /// </code>
  /// <code>
  /// gameObject.scene.GetStream&lt;PreLateUpdate&gt;().Add(deltaTime => {
  ///   // do something inside the game object scene stream in the PreLateUpdate system
  /// });
  /// </code>
  /// </summary>
  public class ExecutionStream {

    /// <inheritdoc cref="ParallelWorkStrategy"/>
    public ParallelWorkStrategy WorkStrategy { get; set; } = ParallelWorkStrategy.Optimal;

    /// <inheritdoc cref="StreamState"/>
    public StreamState State { get; private set; }

    private Action _terminateCallbacks;
    private Action _delayedCallbacks;

    private readonly ActionsStorage _actionsStorage = new();
    private readonly ActionsStorage _parallelActionsStorage = new();

    private readonly ParallelActionsWorker _worker = new();
    private readonly Action<float, int> _handleParallelAction;

    private readonly string _name;
    private readonly string _profilerName;

    internal ExecutionStream(string name) {
      _name = name;
      _profilerName = $"{_name} (stream)";
      _handleParallelAction = HandleParallelAction;
    }

    /// <summary>
    /// Adds a new action to be performed
    /// </summary>
    /// <param name="action"> The action to be performed </param>
    /// <param name="token"> Token for cancelling an action </param>
    /// <param name="priority"> Priority of action execution. Actions with the same priority will be executed in the order they were created. Zero priority is the highest </param>
    /// <exception cref="StreamDisposedException"> Threw if the stream is disposed </exception>
    /// <exception cref="ArgumentNullException"> Threw if the passed action is null </exception>
    public PersistentStreamAction Add(Action<float> action, StreamToken token = default, uint priority = uint.MaxValue) {
      ValidateAddAction(action);

      var streamAction = new PersistentStreamAction(action, token, priority);
      _actionsStorage.Add(streamAction);
      return streamAction;
    }

    /// <summary>
    /// Adds a new action that will be executed in parallel
    /// </summary>
    /// <param name="action"> The action to be performed </param>
    /// <param name="token"> Token for cancelling an action </param>
    /// <exception cref="StreamDisposedException"> Threw if the stream is disposed </exception>
    /// <exception cref="ArgumentNullException"> Threw if the passed action is null </exception>
    public PersistentStreamAction AddParallel([NotNull] Action<float> action, StreamToken token = default) {
      ValidateAddAction(action);

      var streamAction = new PersistentStreamAction(action, token, uint.MaxValue);
      _parallelActionsStorage.Add(streamAction);
      return streamAction;
    }

    /// <summary>
    /// Adds a new action to be performed
    /// </summary>
    /// <param name="time"> The time after which the action will be completed </param>
    /// <param name="action"> The action to be performed </param>
    /// <param name="token"> Token for cancelling an action </param>
    /// <param name="priority"> Priority of action execution. Actions with the same priority will be executed in the order they were created. Zero priority is the highest </param>
    /// <exception cref="StreamDisposedException"> Threw if the stream is disposed </exception>
    /// <exception cref="ArgumentNullException"> Threw if the passed action is null </exception>
    /// <remarks> It is worth distinguishing a temporary action from a timer.
    /// A temporary action is executed every tick of the stream for a specified time,
    /// and a timer executes the action once after the time has elapsed. </remarks>
    public TemporalStreamAction AddTemporal(float time, [NotNull] Action<float> action, StreamToken token = default, uint priority = uint.MaxValue) {
      if (time <= 0) {
        Debug.LogWarning($"Time is negative or zero: {time}");
        return null;
      }

      ValidateAddAction(action);

      var streamAction = new TemporalStreamAction(action, time, token, priority);
      _actionsStorage.Add(streamAction);
      return streamAction;
    }

    /// <summary>
    /// Adds a new action to be performed
    /// </summary>
    /// <param name="condition"> The action will be performed as long as the condition is true </param>
    /// <param name="action"> The action to be performed </param>
    /// <param name="token"> Token for cancelling an action </param>
    /// <param name="priority"> Priority of action execution. Actions with the same priority will be executed in the order they were created. Zero priority is the highest </param>
    /// <exception cref="StreamDisposedException"> Threw if the stream is disposed </exception>
    /// <exception cref="ArgumentNullException"> Threw if the passed action is null </exception>
    public ConditionalStreamAction AddConditional(
      [NotNull] Func<bool> condition, [NotNull] Action<float> action, StreamToken token = default, uint priority = uint.MaxValue
    ) {
      ValidateAddAction(action);
      if (condition == null)
        throw new ArgumentNullException(nameof(condition));

      var streamAction = new ConditionalStreamAction(action, condition, token, priority);
      _actionsStorage.Add(streamAction);
      return streamAction;
    }

    /// <summary>
    /// Adds an action for one-time execution
    /// </summary>
    /// <param name="action"> The action to be performed </param>
    /// <param name="token"> Token for cancelling an action </param>
    /// <param name="priority"> Priority of action execution. Actions with the same priority will be executed in the order they were created. Zero priority is the highest </param>
    /// <exception cref="StreamDisposedException"> Threw if the stream is disposed </exception>
    /// <exception cref="ArgumentNullException"> Threw if the passed action is null </exception>
    public OnceStreamAction AddOnce([NotNull] Action action, StreamToken token = default, uint priority = uint.MaxValue) {
      ValidateAddAction(action);

      var streamAction = new OnceStreamAction(action, token, priority);
      _actionsStorage.Add(streamAction);
      return streamAction;
    }

    /// <summary>
    /// Adds an async action to be performed
    /// </summary>
    /// <param name="action"> The action to be performed </param>
    /// <param name="token"> Token for cancelling an action </param>
    /// <exception cref="StreamDisposedException"> Threw if the stream is disposed </exception>
    /// <exception cref="ArgumentNullException"> Threw if the passed action is null </exception>
    public AsyncStreamAction AddOnce([NotNull] Func<StreamTask> action, StreamToken token = default) {
      ValidateAddAction(action);

      var streamAction = new AsyncStreamAction(action, token);
      _actionsStorage.Add(streamAction);
      return streamAction;
    }

    /// <summary>
    /// Adds an action to be executed once after the specified time has passed
    /// </summary>
    /// <param name="time"> The time after which the action will be executed </param>
    /// <param name="onComplete"> The action which will be executed </param>
    /// <param name="token"> Token for cancelling a timer </param>
    /// <exception cref="StreamDisposedException"> Threw if the stream is disposed </exception>
    /// <exception cref="ArgumentNullException"> Threw if the passed action is null </exception>
    /// <remarks> It is worth distinguishing a timer from a temporary action.
    /// A temporary action is executed every tick of the stream for a specified time,
    /// and a timer executes the action once after the time has elapsed. </remarks>
    public StreamTimer AddTimer(float time, [NotNull] Action onComplete, StreamToken token = default) {
      if (time <= 0)
        throw new ArgumentOutOfRangeException(nameof(time), $"Time is negative or zero: {time}");

      ValidateAddAction(onComplete);

      var streamAction = new StreamTimer(time, onComplete, token);
      _actionsStorage.Add(streamAction);
      return streamAction;
    }

    /// <summary>
    /// Adds the handler that will be called when the stream is terminated
    /// </summary>
    /// <exception cref="ArgumentNullException"> Threw if the passed handler is null </exception>
    /// <remarks> If the stream has already been disposed, the handler will be called immediately </remarks>
    public void OnTerminate([NotNull] Action onTermination, StreamToken subscriptionToken = default) {
      if (onTermination == null)
        throw new ArgumentNullException(nameof(onTermination));

      if (subscriptionToken.Released)
        return;

      if (State == StreamState.Terminated) {
        onTermination();
        return;
      }

      _terminateCallbacks += onTermination;
      subscriptionToken.Register(() => _terminateCallbacks -= onTermination);
    }

    public override string ToString() {
      return _name;
    }

    internal void Update(float deltaTime) {
      ValidateExecution();
      _actionsStorage.Refresh();
      _parallelActionsStorage.Refresh();
      if (!CanExecute())
        return;

      try {
        Execute(deltaTime);
      }
      finally {
        _delayedCallbacks?.Invoke();
        _delayedCallbacks = null;
      }
    }

    internal void Terminate() {
      switch (State) {
        case StreamState.Terminating or StreamState.Terminated:
          return;
        case StreamState.Running:
          _delayedCallbacks += Terminate;
          return;
      }

      State = StreamState.Terminating;
      _actionsStorage.Clear();
      _parallelActionsStorage.Clear();
      _terminateCallbacks?.Invoke();
      _terminateCallbacks = null;
      _delayedCallbacks = null;
      State = StreamState.Terminated;
    }

    protected virtual bool CanExecute() {
      return _actionsStorage.Count != 0 || _parallelActionsStorage.Count != 0;
    }

    protected void CopyFrom(ExecutionStream other) {
      _actionsStorage.CopyFrom(other._actionsStorage);
      _parallelActionsStorage.CopyFrom(other._parallelActionsStorage);
      _delayedCallbacks += other._delayedCallbacks;
      _terminateCallbacks += other._terminateCallbacks;
    }

    private void ValidateExecution() {
      switch (State) {
        case StreamState.Terminating or StreamState.Terminated:
          throw new StreamsException("Cannot execute disposed stream");
        case StreamState.Running:
          State = StreamState.Invalid;
          Terminate();
          throw new StreamsException("Recursive execution occurred");
        case StreamState.Invalid:
          Terminate();
          throw new StreamsException("Stream is in invalid state");
        case StreamState.Idle:
          break;
        default:
          throw new ArgumentOutOfRangeException();
      }
    }

    private void Execute(float deltaTime) {
      State = StreamState.Running;
      Streams.PushStream(this);
      Profiler.BeginSample(_profilerName);

      _worker.Start(deltaTime, _parallelActionsStorage.Count, WorkStrategy, _handleParallelAction);

      for (var i = 0; i < _actionsStorage.Count; i++)
        HandleAction(deltaTime, _actionsStorage, i);

      _worker.Wait();

      Profiler.EndSample();
      Streams.PopStream();
      State = StreamState.Idle;
    }

    private void HandleParallelAction(float deltaTime, int index) {
      HandleAction(deltaTime, _parallelActionsStorage, index);
    }

    private void HandleAction(float deltaTime, ActionsStorage storage, int index) {
      StreamAction action = storage[index];

      try {
        action.Invoke(deltaTime);
      }
      catch (Exception exception) {
        Debug.LogError($"An error occured while executing action <b>{action}</b>");
        Debug.LogException(exception);
        storage.Remove(action);
      }
    }

    private void ValidateAddAction(Delegate action) {
      switch (State) {
        case StreamState.Terminating or StreamState.Terminated:
          throw new StreamDisposedException(ToString());
      }

      if (action == null)
        throw new ArgumentNullException(nameof(action));
    }

  }

}