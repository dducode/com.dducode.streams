using System;
using System.Collections.Generic;
using System.Threading;
using JetBrains.Annotations;
using Streams.Exceptions;
using Streams.Internal;
using Streams.StreamActions;
using Streams.StreamTasks;
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

    private readonly List<IDisposable> _disposables;

    private readonly ActionsStorage _invokablesStorage = new();
    private readonly ActionsStorage _concurrentInvokablesStorage = new() { Sorted = false };
    private readonly ActionsStorage _taskSourcesStorage = new() { Sorted = false };

    private readonly ContinuationsHandler _continuationsHandler = new(StreamToken.None);
    private readonly ContinuationsHandler _concurrentContinuationsHandler = new(StreamToken.None);

    private readonly ParallelActionsWorker _worker = new();
    private readonly Action<float, int> _handleConcurrentInvokable;

    private readonly string _name;

    internal ExecutionStream(string name) {
      _name = $"{name} (stream)";
      _handleConcurrentInvokable = (deltaTime, index) => {
        HandleInvokable(deltaTime, _concurrentInvokablesStorage, _concurrentInvokablesStorage[index]);
      };

      _disposables = new List<IDisposable>(new[] {
        _invokablesStorage, _concurrentInvokablesStorage, _taskSourcesStorage
      });

      _concurrentInvokablesStorage.Add(_concurrentContinuationsHandler);
    }

    /// <summary>
    /// Adds a new action to be performed
    /// </summary>
    /// <param name="action"> The action to be performed </param>
    /// <param name="token"> Token for cancelling an action </param>
    /// <exception cref="StreamDisposedException"> Is thrown if the stream is disposed </exception>
    /// <exception cref="ArgumentNullException"> Is thrown if the passed action is null </exception>
    public IConfigurable Add([NotNull] Action<float> action, StreamToken token = default) {
      ValidateAdd(action);

      var streamAction = new StreamAction(action, token);
      _invokablesStorage.Add(streamAction);
      return streamAction;
    }

    /// <summary>
    /// Adds an async action to be performed
    /// </summary>
    /// <param name="action"> The action to be performed </param>
    /// <param name="token"> Token for cancelling an action </param>
    /// <exception cref="StreamDisposedException"> Is thrown if the stream is disposed </exception>
    /// <exception cref="ArgumentNullException"> Is thrown if the passed action is null </exception>
    public void Add([NotNull] Func<StreamTask> action, StreamToken token = default) {
      ValidateAdd(action);

      var asyncAction = new AsyncAction(action, token);
      _invokablesStorage.Add(asyncAction);
    }

    /// <summary>
    /// Adds an object to run in the current stream
    /// </summary>
    public void Add([NotNull] IInvokable invokable) {
      ValidateAdd(invokable);
      _invokablesStorage.Add(invokable);
    }

    /// <summary>
    /// Adds a new action that will be executed in parallel
    /// </summary>
    /// <param name="action"> The action to be performed </param>
    /// <param name="token"> Token for cancelling an action </param>
    /// <exception cref="StreamDisposedException"> Is thrown if the stream is disposed </exception>
    /// <exception cref="ArgumentNullException"> Is thrown if the passed action is null </exception>
    public IConfigurable AddConcurrent([NotNull] Action<float> action, StreamToken token = default) {
      ValidateAdd(action);

      var streamAction = new StreamAction(action, token);
      _concurrentInvokablesStorage.Add(streamAction);
      return streamAction;
    }

    /// <summary>
    /// Adds an async action to be performed in parallel
    /// </summary>
    /// <param name="action"> The action to be performed </param>
    /// <param name="token"> Token for cancelling an action </param>
    /// <exception cref="StreamDisposedException"> Is thrown if the stream is disposed </exception>
    /// <exception cref="ArgumentNullException"> Is thrown if the passed action is null </exception>
    public void AddConcurrent([NotNull] Func<StreamTask> action, StreamToken token = default) {
      ValidateAdd(action);

      var asyncAction = new AsyncAction(action, token);
      _concurrentInvokablesStorage.Add(asyncAction);
    }

    /// <summary>
    /// Adds an object to run in the current stream in parallel
    /// </summary>
    public void AddConcurrent([NotNull] IInvokable invokable) {
      ValidateAdd(invokable);
      _concurrentInvokablesStorage.Add(invokable);
    }

    /// <summary>
    /// Adds an action for one-time execution
    /// </summary>
    /// <param name="action"> The action to be performed </param>
    /// <param name="token"> Token for cancelling an action </param>
    /// <exception cref="StreamDisposedException"> Is thrown if the stream is disposed </exception>
    /// <exception cref="ArgumentNullException"> Is thrown if the passed action is null </exception>
    public ICompletable AddOnce([NotNull] Action action, StreamToken token = default) {
      ValidateAdd(action);

      var onceAction = new OnceAction(action, token);
      _invokablesStorage.Add(onceAction);
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
      ValidateAdd(action);

      var asyncOnceAction = new AsyncOnceAction(action, token);
      _invokablesStorage.Add(asyncOnceAction);
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

      ValidateAdd(action);

      var delayedAction = new DelayedAction(time, action, token);
      _invokablesStorage.Add(delayedAction);
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
      _invokablesStorage.CopyFrom(other._invokablesStorage);
      _concurrentInvokablesStorage.CopyFrom(other._concurrentInvokablesStorage);
      _taskSourcesStorage.CopyFrom(other._taskSourcesStorage);

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
      if (!Thread.CurrentThread.IsBackground)
        _taskSourcesStorage.Add(invokable);
      else
        _concurrentInvokablesStorage.Add(invokable);
    }

    internal void ScheduleContinuation(Action continuation) {
      ValidateStreamState();
      if (!Thread.CurrentThread.IsBackground)
        _continuationsHandler.Enqueue(continuation);
      else
        _concurrentContinuationsHandler.Enqueue(continuation);
    }

    internal virtual void Update(float deltaTime) {
      ValidateExecution();

      try {
        State = StreamState.Running;
        _streamsStack.Push(this);
        Profiler.BeginSample(_name);

        Execute(deltaTime);
      }
      finally {
        _delayedCallbacks?.Invoke();
        _delayedCallbacks = null;

        Profiler.EndSample();
        _streamsStack.Pop();
        State = StreamState.Idle;
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
      _streamsStack.Push(this);

      try {
        _terminateCallbacks?.Invoke();
      }
      catch (Exception e) {
        Debug.LogError("An error occurred while executing terminate callbacks");
        Debug.LogException(e);
      }

      _terminateCallbacks = null;
      _delayedCallbacks = null;

      foreach (IDisposable disposable in _disposables)
        disposable.Dispose();

      _disposables.Clear();
      _streamsStack.Pop();
      State = StreamState.Terminated;
    }

    private protected virtual void Clear() {
      switch (State) {
        case StreamState.Terminating or StreamState.Terminated:
          return;
        case StreamState.Running:
          _delayedCallbacks += Clear;
          return;
      }

      _invokablesStorage.Clear();
      _concurrentInvokablesStorage.Clear();
      _taskSourcesStorage.Clear();

      _continuationsHandler.Dispose();

      _terminateCallbacks = null;
      _delayedCallbacks = null;

      _disposables.Clear();
      State = StreamState.Terminated;
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
      _continuationsHandler.Invoke(deltaTime);

      _concurrentInvokablesStorage.ApplyAdding();
      _worker.Start(deltaTime, _concurrentInvokablesStorage.Count, WorkStrategy, _handleConcurrentInvokable);

      HandleStorage(deltaTime, _taskSourcesStorage);
      HandleStorage(deltaTime, _invokablesStorage);

      _worker.Wait();
      _concurrentInvokablesStorage.ApplyRemoving();
    }

    private void HandleStorage(float deltaTime, ActionsStorage storage) {
      foreach (IInvokable invokable in storage)
        HandleInvokable(deltaTime, storage, invokable);
    }

    private void HandleInvokable(float deltaTime, ActionsStorage storage, IInvokable invokable) {
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

    private void ValidateAdd(object action) {
      ValidateStreamState();
      if (action == null)
        throw new ArgumentNullException(nameof(action));
    }

  }

}