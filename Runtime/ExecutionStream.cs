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

    private static readonly Stack<ExecutionStream> _streamsStack = new();

    private Action _terminateCallbacks;
    private Action _delayedCallbacks;

    private readonly List<ActionsStorage> _allStorages;
    private readonly List<IDisposable> _disposables;
    private readonly ActionsStorage _actionsStorage = new();
    private readonly ActionsStorage _parallelActionsStorage = new() { Sorted = false };
    private readonly ActionsStorage _taskSourcesStorage = new() { Sorted = false };

    private ContinuationsHandler _continuationsHandler;
    private Initializer _initializer;

    private readonly ParallelActionsWorker _worker = new();
    private readonly Action<float, int> _handleParallelAction;

    private readonly string _name;
    private readonly string _profilerName;

    internal ExecutionStream(string name) {
      _name = name;
      _profilerName = $"{_name} (stream)";
      _handleParallelAction = HandleParallelInvokable;
      _allStorages = new List<ActionsStorage>(new[] {
        _actionsStorage, _parallelActionsStorage, _taskSourcesStorage
      });
      _disposables = new List<IDisposable>(new[] {
        _actionsStorage, _parallelActionsStorage, _taskSourcesStorage
      });
    }

    /// <summary>
    /// Adds a new action to be performed
    /// </summary>
    /// <param name="action"> The action to be performed </param>
    /// <param name="token"> Token for cancelling an action </param>
    /// <exception cref="StreamDisposedException"> Is thrown if the stream is disposed </exception>
    /// <exception cref="ArgumentNullException"> Is thrown if the passed action is null </exception>
    public IConfigurable Add([NotNull] Action<float> action, StreamToken token = default) {
      ValidateAddAction(action);

      var streamAction = new StreamAction(action, token);
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
    public void Add([NotNull] Func<CashedTask> action, StreamToken token = default) {
      ValidateAddAction(action);

      var asyncAction = new AsyncAction(action, token);
      ScheduleInitializable(asyncAction);
      _actionsStorage.Add(asyncAction);
    }

    /// <summary>
    /// Adds a new action that will be executed in parallel
    /// </summary>
    /// <param name="action"> The action to be performed </param>
    /// <param name="token"> Token for cancelling an action </param>
    /// <exception cref="StreamDisposedException"> Is thrown if the stream is disposed </exception>
    /// <exception cref="ArgumentNullException"> Is thrown if the passed action is null </exception>
    public IConfigurable AddConcurrent([NotNull] Action<float> action, StreamToken token = default) {
      ValidateAddAction(action);

      var streamAction = new StreamAction(action, token);
      _parallelActionsStorage.Add(streamAction);
      return streamAction;
    }

    /// <summary>
    /// Adds an async action to be performed in parallel
    /// </summary>
    /// <param name="action"> The action to be performed </param>
    /// <param name="token"> Token for cancelling an action </param>
    /// <exception cref="StreamDisposedException"> Is thrown if the stream is disposed </exception>
    /// <exception cref="ArgumentNullException"> Is thrown if the passed action is null </exception>
    public void AddConcurrent([NotNull] Func<CashedTask> action, StreamToken token = default) {
      ValidateAddAction(action);

      var asyncAction = new AsyncAction(action, token);
      ScheduleInitializable(asyncAction);
      _parallelActionsStorage.Add(asyncAction);
    }

    /// <summary>
    /// Adds an action for one-time execution
    /// </summary>
    /// <param name="action"> The action to be performed </param>
    /// <param name="token"> Token for cancelling an action </param>
    /// <exception cref="StreamDisposedException"> Is thrown if the stream is disposed </exception>
    /// <exception cref="ArgumentNullException"> Is thrown if the passed action is null </exception>
    public ICompletable AddOnce([NotNull] Action action, StreamToken token = default) {
      ValidateAddAction(action);

      var onceAction = new OnceAction(action, token);
      _actionsStorage.Add(onceAction);
      return onceAction;
    }

    /// <summary>
    /// Adds an async action to be performed
    /// </summary>
    /// <param name="action"> The action to be performed </param>
    /// <param name="token"> Token for cancelling an action </param>
    /// <exception cref="StreamDisposedException"> Is thrown if the stream is disposed </exception>
    /// <exception cref="ArgumentNullException"> Is thrown if the passed action is null </exception>
    public ICompletable AddOnce([NotNull] Func<StreamTask> action, StreamToken token = default) {
      ValidateAddAction(action);

      var asyncOnceAction = new AsyncOnceAction(action, token);
      ScheduleInitializable(asyncOnceAction);
      _actionsStorage.Add(asyncOnceAction);
      return asyncOnceAction;
    }

    /// <summary>
    /// Adds an action to be executed once after the specified time has passed
    /// </summary>
    /// <param name="time"> The time after which the action will be executed </param>
    /// <param name="action"> The action which will be executed </param>
    /// <param name="token"> Token for cancelling a timer </param>
    /// <exception cref="StreamDisposedException"> Is thrown if the stream is disposed </exception>
    /// <exception cref="ArgumentNullException"> Is thrown if the passed action is null </exception>
    /// <remarks> It is worth distinguishing a timer from a temporary action.
    /// A temporary action is executed every tick of the stream for a specified time,
    /// and a timer executes the action once after the time has elapsed. </remarks>
    public ICompletable AddDelayed(float time, [NotNull] Action action, StreamToken token = default) {
      if (time < 0)
        throw new ArgumentOutOfRangeException(nameof(time), $"Time is negative: {time}");

      ValidateAddAction(action);

      var delayedAction = new DelayedAction(time, action, token);
      _actionsStorage.Add(delayedAction);
      return delayedAction;
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
      _taskSourcesStorage.CopyFrom(other._taskSourcesStorage);

      _initializer.CopyFrom(other._initializer);
      _continuationsHandler.CopyFrom(other._continuationsHandler);

      _delayedCallbacks += other._delayedCallbacks;
      _terminateCallbacks += other._terminateCallbacks;
    }

    protected void ValidateStreamState() {
      if (State is StreamState.Terminating or StreamState.Terminated)
        throw new StreamDisposedException(ToString());
    }

    internal void AddInvokableTaskSource(IInvokable invokable) {
      ValidateStreamState();
      _taskSourcesStorage.Add(invokable);
    }

    internal void ScheduleContinuation(Action continuation) {
      ValidateStreamState();
      if (_continuationsHandler == null) {
        _continuationsHandler = new ContinuationsHandler(StreamToken.None);
        _actionsStorage.Add(_continuationsHandler);
        _disposables.Add(_continuationsHandler);
      }

      _continuationsHandler.Enqueue(continuation);
    }

    internal virtual void Update(float deltaTime) {
      ValidateExecution();

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
      foreach (IDisposable disposable in _disposables)
        disposable.Dispose();

      _allStorages.Clear();
      _disposables.Clear();

      _terminateCallbacks?.Invoke();
      _terminateCallbacks = null;
      _delayedCallbacks = null;
      State = StreamState.Terminated;
    }

    private void ScheduleInitializable(IInitializable initializable) {
      ValidateStreamState();
      if (_initializer == null) {
        _initializer = new Initializer(StreamToken.None);
        _disposables.Add(_initializer);
      }

      _initializer.Enqueue(initializable);
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

      _initializer.Invoke(deltaTime);

      while (_initializer.corruptedObjects.TryDequeue(out IInitializable corrupted))
        if (corrupted is IInvokable invokable)
          foreach (ActionsStorage storage in _allStorages)
            storage.Remove(invokable);

      foreach (ActionsStorage storage in _allStorages)
        storage.Refresh();

      _worker.Start(deltaTime, _parallelActionsStorage.Count, WorkStrategy, _handleParallelAction);

      for (var i = 0; i < _taskSourcesStorage.Count; i++)
        HandleInvokable(deltaTime, _taskSourcesStorage, i);
      for (var i = 0; i < _actionsStorage.Count; i++)
        HandleInvokable(deltaTime, _actionsStorage, i);

      _worker.Wait();

      Profiler.EndSample();
      _streamsStack.Pop();
      State = StreamState.Idle;
    }

    private void HandleParallelInvokable(float deltaTime, int index) {
      HandleInvokable(deltaTime, _parallelActionsStorage, index);
    }

    private void HandleInvokable(float deltaTime, ActionsStorage storage, int index) {
      IInvokable invokable = storage[index];

      try {
        if (!invokable.Invoke(deltaTime))
          storage.Remove(invokable);
      }
      catch (Exception exception) {
        Debug.LogError($"An error occured while executing action <b>{invokable}</b>");
        Debug.LogException(exception);
        storage.Remove(invokable);
      }
    }

    private void ValidateAddAction(Delegate action) {
      ValidateStreamState();
      if (action == null)
        throw new ArgumentNullException(nameof(action));
    }

  }

}