using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using Streams.StreamContexts;
using Streams.StreamTasks.Internal;
using Streams.StreamTasks.TaskSources;

namespace Streams.StreamTasks {

  public partial struct StreamTask {

    public static StreamYieldAwaitable Yield() {
      return new StreamYieldAwaitable(StreamTaskHelper.GetRunningStream());
    }

    public static StreamYieldAwaitable ContinueOnStream<TSystemType>() {
      return new StreamYieldAwaitable(ExecutionContexts.All.GetValueOrDefault(StreamTaskHelper.GetRunningStream()).GetStream<TSystemType>());
    }

    public static StreamTask Delay(int milliseconds) {
      if (milliseconds < 0)
        throw new ArgumentOutOfRangeException(nameof(milliseconds));
      if (milliseconds == 0)
        return CompletedTask;

      if (!TaskSourcePool.TryGet(out DelayedTaskSource source))
        source = new DelayedTaskSource();

      source.Setup(milliseconds / 1000f);
      StreamTaskHelper.GetRunningStream().AddInvokableTaskSource(source);
      return source.Task;
    }

    public static StreamTask WaitWhile([NotNull] Func<bool> condition) {
      if (condition == null)
        throw new ArgumentNullException(nameof(condition));
      if (!condition())
        return CompletedTask;

      if (!TaskSourcePool.TryGet(out ConditionalTaskSource source))
        source = new ConditionalTaskSource();

      source.Setup(condition);
      StreamTaskHelper.GetRunningStream().AddInvokableTaskSource(source);
      return source.Task;
    }

    public static StreamTask WhenAll([NotNull] params StreamTask[] tasks) {
      if (tasks == null)
        throw new ArgumentNullException(nameof(tasks));
      if (tasks.Length == 0)
        return CompletedTask;

      if (!TaskSourcePool.TryGet(out EveryoneWaitingTaskSource source))
        source = new EveryoneWaitingTaskSource();

      source.Setup(tasks);
      StreamTaskHelper.GetRunningStream().AddInvokableTaskSource(source);
      return source.Task;
    }

    public static StreamTask WhenAny([NotNull] params StreamTask[] tasks) {
      if (tasks == null)
        throw new ArgumentNullException(nameof(tasks));
      if (tasks.Length == 0)
        return CompletedTask;

      if (!TaskSourcePool.TryGet(out AnyoneWaitingTaskSource source))
        source = new AnyoneWaitingTaskSource();

      source.Setup(tasks);
      StreamTaskHelper.GetRunningStream().AddInvokableTaskSource(source);
      return source.Task;
    }

  }

}