using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using Streams.Exceptions;
using Streams.Internal;
using Streams.StreamActions;
using Streams.StreamTasks;
using Streams.StreamTasks.Internal;
using UnityEngine.Profiling;
using Debug = UnityEngine.Debug;

namespace Streams {

  /// <summary>
  /// <p> Base class for all streams in the system. Execution stream contains and executes any actions. </p>
  /// <p> Base streams executed on the Player Loop Systems, and they can be obtained from <see cref="UnityPlayerLoop.GetStream{TSystem}"/> method.
  /// If you want to get the stream from scene, you can call <see cref="SceneStreams.GetStream{TBaseSystem}"/> </p>
  /// <code>
  /// UnityPlayerLoop.Get&lt;Update&gt;().Add(self => {
  ///   // do something inside the base stream in the Update system
  /// });
  /// </code>
  /// <code>
  /// SceneManager.GetActiveScene().GetStream&lt;FixedUpdate&gt;().Add(self => {
  ///   // do something inside the active scene stream in the FixedUpdate system
  /// });
  /// </code>
  /// <code>
  /// gameObject.GetStream&lt;PreLateUpdate&gt;().Add(self => {
  ///   // do something inside the game object stream in the PreLateUpdate system
  /// });
  /// </code>
  /// </summary>
  public class ExecutionStream {

    /// <summary>
    /// Gets the currently running stream. If you try to get this outside of any stream, you'll get null
    /// </summary>
    [CanBeNull]
    public static ExecutionStream RunningStream => _streamsStack.Count == 0 ? null : _streamsStack.Peek();

    /// <inheritdoc cref="ParallelWorkStrategy"/>
    public ParallelWorkStrategy WorkStrategy { get; set; } = ParallelWorkStrategy.Optimal;

    /// <inheritdoc cref="StreamState"/>
    public StreamState State { get; private set; }

    public bool Locked => _lockers > 0;

    private static readonly Stack<ExecutionStream> _streamsStack = new();

    private Action _terminateCallbacks;
    private Action _delayedCallbacks;

    private readonly ActionsStorage _actionsStorage = new();
    private readonly ActionsStorage _parallelActionsStorage = new();
    private readonly Queue<StreamTask> _waitedTasks = new();
    private readonly Queue<Action> _continuations = new();

    private readonly ParallelActionsWorker _worker = new();
    private readonly Action<float, int> _handleParallelAction;

    private readonly string _name;
    private readonly string _profilerName;

    private int _lockers;

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
    /// <exception cref="StreamDisposedException"> Is thrown if the stream is disposed </exception>
    /// <exception cref="ArgumentNullException"> Is thrown if the passed action is null </exception>
    public PersistentAction Add([NotNull] Action<PersistentAction> action, StreamToken token = default) {
      ValidateAddAction(action);

      var streamAction = new PersistentAction(action, token);
      _actionsStorage.Add(streamAction);
      return streamAction;
    }

    /// <summary>
    /// Adds a new action to be performed
    /// </summary>
    /// <param name="condition"> The action will be performed when the condition is true </param>
    /// <param name="action"> The action to be performed </param>
    /// <param name="token"> Token for cancelling an action </param>
    /// <exception cref="StreamDisposedException"> Is thrown if the stream is disposed </exception>
    /// <exception cref="ArgumentNullException"> Is thrown if the passed action is null </exception>
    public ConditionalAction Add(
      [NotNull] Func<bool> condition, [NotNull] Action<ConditionalAction> action, StreamToken token = default) {
      ValidateAddAction(action);
      if (condition == null)
        throw new ArgumentNullException(nameof(condition));

      var streamAction = new ConditionalAction(action, condition, token);
      _actionsStorage.Add(streamAction);
      return streamAction;
    }

    /// <summary>
    /// Adds a new action to be performed
    /// </summary>
    /// <param name="time"> The time after which the action will be completed </param>
    /// <param name="action"> The action to be performed </param>
    /// <param name="token"> Token for cancelling an action </param>
    /// <exception cref="StreamDisposedException"> Is thrown if the stream is disposed </exception>
    /// <exception cref="ArgumentNullException"> Is thrown if the passed action is null </exception>
    /// <remarks> It is worth distinguishing a temporary action from a timer.
    /// A temporary action is executed every tick of the stream for a specified time,
    /// and a timer executes the action once after the time has elapsed. </remarks>
    public TemporalAction Add(float time, [NotNull] Action<TemporalAction> action, StreamToken token = default) {
      if (time <= 0) {
        Debug.LogWarning($"Time is negative or zero: {time}");
        return null;
      }

      ValidateAddAction(action);

      var streamAction = new TemporalAction(action, time, token);
      _actionsStorage.Add(streamAction);
      return streamAction;
    }

    /// <summary>
    /// Adds an async action to be performed
    /// </summary>
    /// <param name="action"> The action to be performed </param>
    /// <param name="token"> Token for cancelling an action </param>
    /// <exception cref="StreamDisposedException"> Is thrown if the stream is disposed </exception>
    /// <exception cref="ArgumentNullException"> Is thrown if the passed action is null </exception>
    public AsyncAction Add([NotNull] Func<AsyncAction, RestartableStreamTask> action, StreamToken token = default) {
      ValidateAddAction(action);

      var streamAction = new AsyncAction(action, token);
      _actionsStorage.Add(streamAction);
      return streamAction;
    }

    /// <summary>
    /// Adds a new action that will be executed in parallel
    /// </summary>
    /// <param name="action"> The action to be performed </param>
    /// <param name="token"> Token for cancelling an action </param>
    /// <exception cref="StreamDisposedException"> Is thrown if the stream is disposed </exception>
    /// <exception cref="ArgumentNullException"> Is thrown if the passed action is null </exception>
    public PersistentAction AddParallel([NotNull] Action<PersistentAction> action, StreamToken token = default) {
      ValidateAddAction(action);

      var streamAction = new PersistentAction(action, token);
      _parallelActionsStorage.Add(streamAction);
      return streamAction;
    }

    /// <summary>
    /// Adds an action for one-time execution
    /// </summary>
    /// <param name="action"> The action to be performed </param>
    /// <param name="token"> Token for cancelling an action </param>
    /// <exception cref="StreamDisposedException"> Is thrown if the stream is disposed </exception>
    /// <exception cref="ArgumentNullException"> Is thrown if the passed action is null </exception>
    public OnceAction AddOnce([NotNull] Action action, StreamToken token = default) {
      ValidateAddAction(action);

      var streamAction = new OnceAction(action, token);
      _actionsStorage.Add(streamAction);
      return streamAction;
    }

    /// <summary>
    /// Adds an async action to be performed
    /// </summary>
    /// <param name="action"> The action to be performed </param>
    /// <param name="token"> Token for cancelling an action </param>
    /// <exception cref="StreamDisposedException"> Is thrown if the stream is disposed </exception>
    /// <exception cref="ArgumentNullException"> Is thrown if the passed action is null </exception>
    public AsyncOnceAction AddOnce([NotNull] Func<StreamTask> action, StreamToken token = default) {
      ValidateAddAction(action);

      var streamAction = new AsyncOnceAction(action, token);
      _actionsStorage.Add(streamAction);
      return streamAction;
    }

    /// <summary>
    /// Adds an action to be executed once after the specified time has passed
    /// </summary>
    /// <param name="time"> The time after which the action will be executed </param>
    /// <param name="onComplete"> The action which will be executed </param>
    /// <param name="token"> Token for cancelling a timer </param>
    /// <exception cref="StreamDisposedException"> Is thrown if the stream is disposed </exception>
    /// <exception cref="ArgumentNullException"> Is thrown if the passed action is null </exception>
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
    /// Call this to lock the stream on a token
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Occurs when attempting to lock a stream inside its execution. Example:
    /// <code>
    /// stream.AddOnce(() => stream.Lock(token));
    /// </code>
    /// </exception>
    /// <exception cref="StreamDisposedException"> Is thrown if the stream is disposed </exception>
    public void Lock(StreamToken lockToken) {
      if (State == StreamState.Running)
        throw new InvalidOperationException("Cannot lock a stream inside its execution");

      ValidateStreamState();
      _lockers++;
      lockToken.Register(() => _lockers--);
    }

    /// <summary>
    /// Adds the handler that will be called when the stream is terminated
    /// </summary>
    /// <remarks> If the stream has already been disposed, the handler will be called immediately </remarks>
    /// <exception cref="ArgumentNullException"> Is thrown if the passed handler is null </exception>
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

    protected void CopyFrom(ExecutionStream other) {
      _actionsStorage.CopyFrom(other._actionsStorage);
      _parallelActionsStorage.CopyFrom(other._parallelActionsStorage);
      _delayedCallbacks += other._delayedCallbacks;
      _terminateCallbacks += other._terminateCallbacks;
    }

    protected void ValidateStreamState() {
      if (State is StreamState.Terminating or StreamState.Terminated)
        throw new StreamDisposedException(ToString());
    }

    internal void ScheduleContinuation(Action continuation) {
      _continuations.Enqueue(continuation);
    }

    internal void ScheduleTaskCompletion(StreamTask task) {
      _waitedTasks.Enqueue(task);
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

    private bool CanExecute() {
      return (_actionsStorage.Count != 0 || _parallelActionsStorage.Count != 0 || _waitedTasks.Count != 0 || _continuations.Count != 0) && !Locked;
    }

    private void ValidateExecution() {
      switch (State) {
        case StreamState.Terminating or StreamState.Terminated:
          throw new StreamDisposedException(ToString());
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
      _streamsStack.Push(this);
      Profiler.BeginSample(_profilerName);

      while (_waitedTasks.TryDequeue(out StreamTask task))
        task.SetResult();

      while (_continuations.TryDequeue(out Action continuation))
        continuation();

      _worker.Start(deltaTime, _parallelActionsStorage.Count, WorkStrategy, _handleParallelAction);

      for (var i = 0; i < _actionsStorage.Count; i++)
        HandleAction(deltaTime, _actionsStorage, i);

      _worker.Wait();

      Profiler.EndSample();
      _streamsStack.Pop();
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
      ValidateStreamState();
      if (action == null)
        throw new ArgumentNullException(nameof(action));
    }

  }

}