using System;
using System.Diagnostics.CodeAnalysis;
using StreamsForUnity.Exceptions;
using StreamsForUnity.Internal;
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

    protected Action disposeCallbacks;
    protected Action delayedCallbacks;

    private protected readonly ActionsStorage actionsStorage = new();
    private protected readonly ActionsStorage parallelActionsStorage = new();

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
    public StreamAction Add(Action<float> action, StreamToken token = default, uint priority = uint.MaxValue) {
      ValidateAddAction(action);

      var streamAction = new StreamAction(action, float.PositiveInfinity, token, priority);
      actionsStorage.Add(streamAction);
      return streamAction;
    }

    /// <summary>
    /// Adds a new action that will be executed in parallel
    /// </summary>
    /// <param name="action"> The action to be performed </param>
    /// <param name="token"> Token for cancelling an action </param>
    /// <exception cref="StreamDisposedException"> Threw if the stream is disposed </exception>
    /// <exception cref="ArgumentNullException"> Threw if the passed action is null </exception>
    public StreamAction AddParallel([NotNull] Action<float> action, StreamToken token = default) {
      ValidateAddAction(action);

      var streamAction = new StreamAction(action, float.PositiveInfinity, token, uint.MaxValue);
      parallelActionsStorage.Add(streamAction);
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
    public StreamAction AddTemporary(float time, [NotNull] Action<float> action, StreamToken token = default, uint priority = uint.MaxValue) {
      if (time <= 0) {
        Debug.LogWarning($"Time is negative or zero: {time}");
        return null;
      }

      ValidateAddAction(action);

      var streamAction = new StreamAction(action, time, token, priority);
      actionsStorage.Add(streamAction);
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
    public StreamAction AddConditional(
      [NotNull] Func<bool> condition, [NotNull] Action<float> action, StreamToken token = default, uint priority = uint.MaxValue
    ) {
      ValidateAddAction(action);
      if (condition == null)
        throw new ArgumentNullException(nameof(condition));

      var sts = new StreamTokenSource();
      var streamAction = new StreamAction(deltaTime => {
        if (condition())
          action(deltaTime);
        else
          sts.Release();
      }, float.PositiveInfinity, token, priority);
      streamAction.SetCompletionToken(sts.Token);
      actionsStorage.Add(streamAction);
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
    public void AddOnce([NotNull] Action action, StreamToken token = default, uint priority = uint.MaxValue) {
      ValidateAddAction(action);

      actionsStorage.Add(new StreamAction(_ => action(), float.Epsilon, token, priority));
    }

    /// <summary>
    /// Adds an async action to be performed
    /// </summary>
    /// <param name="action"> The action to be performed </param>
    /// <param name="token"> Token for cancelling an action </param>
    /// <exception cref="StreamDisposedException"> Threw if the stream is disposed </exception>
    /// <exception cref="ArgumentNullException"> Threw if the passed action is null </exception>
    public void AddOnce([NotNull] Func<StreamTask> action, StreamToken token = default) {
      ValidateAddAction(action);

      actionsStorage.Add(new StreamAction(_ => action(), float.Epsilon, token, uint.MaxValue));
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
    public void AddTimer(float time, [NotNull] Action onComplete, StreamToken token = default) {
      if (time <= 0) {
        Debug.LogWarning($"Time is negative or zero: {time}");
        return;
      }

      ValidateAddAction(onComplete);

      var streamAction = new StreamAction(_ => { }, time, token, uint.MaxValue);
      actionsStorage.Add(streamAction);
      streamAction.OnComplete(() => {
        if (State is StreamState.Terminating or StreamState.Terminated)
          return;
        AddOnce(onComplete, token);
      });
    }

    /// <summary>
    /// Adds the handler that will be called when the stream is disposed
    /// </summary>
    /// <param name="onDispose"> Dispose handler </param>
    /// <exception cref="ArgumentNullException"> Threw if the passed handler is null </exception>
    /// <remarks> If the stream has already been disposed, the handler will be called immediately </remarks>
    public void OnDispose([NotNull] Action onDispose) {
      if (onDispose == null)
        throw new ArgumentNullException(nameof(onDispose));

      if (State == StreamState.Terminated) {
        onDispose();
        return;
      }

      disposeCallbacks += onDispose;
    }

    public override string ToString() {
      return _name;
    }

    internal void RemoveDisposeHandle([NotNull] Action onDispose) {
      disposeCallbacks -= onDispose ?? throw new ArgumentNullException(nameof(onDispose));
    }

    internal void Update(float deltaTime) {
      ValidateExecution();
      actionsStorage.Refresh();
      parallelActionsStorage.Refresh();
      if (!CanExecute())
        return;

      try {
        Execute(deltaTime);
      }
      finally {
        delayedCallbacks?.Invoke();
        delayedCallbacks = null;
      }
    }

    protected virtual bool CanExecute() {
      return actionsStorage.Count != 0 || parallelActionsStorage.Count != 0;
    }

    internal void Terminate() {
      switch (State) {
        case StreamState.Terminating or StreamState.Terminated:
          return;
        case StreamState.Running:
          delayedCallbacks += Terminate;
          return;
      }

      State = StreamState.Terminating;
      actionsStorage.Clear();
      parallelActionsStorage.Clear();
      disposeCallbacks?.Invoke();
      disposeCallbacks = null;
      delayedCallbacks = null;
      State = StreamState.Terminated;
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

      _worker.Start(deltaTime, parallelActionsStorage.Count, WorkStrategy, _handleParallelAction);

      for (var i = 0; i < actionsStorage.Count; i++)
        HandleAction(deltaTime, actionsStorage, i);

      _worker.Wait();

      Profiler.EndSample();
      Streams.PopStream();
      State = StreamState.Idle;
    }

    private void HandleParallelAction(float deltaTime, int index) {
      HandleAction(deltaTime, parallelActionsStorage, index);
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