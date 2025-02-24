using System;
using JetBrains.Annotations;
using StreamsForUnity.StreamTasks;

namespace StreamsForUnity {

  public interface IExecutionStream {

    public StreamState StreamState { get; }
    public StreamAction Add([NotNull] Action<float> action, StreamToken token = default, uint priority = uint.MaxValue);
    public StreamAction AddParallel([NotNull] Action<float> action, StreamToken token = default);
    public StreamAction AddTemporary(float time, [NotNull] Action<float> action, StreamToken token = default, uint priority = uint.MaxValue);
    public StreamAction AddConditional(
      [NotNull] Func<bool> condition, [NotNull] Action<float> action, StreamToken token = default, uint priority = uint.MaxValue
    );
    public void AddOnce([NotNull] Action action, StreamToken token = default, uint priority = uint.MaxValue);
    public void AddOnce([NotNull] Func<StreamTask> action, StreamToken token = default);
    public void AddTimer(float time, [NotNull] Action onComplete, StreamToken token = default);
    public void OnDispose(Action onDispose);

  }

}