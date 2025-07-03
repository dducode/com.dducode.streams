#if STREAMS_UNITASK_INTEGRATION
using Cysharp.Threading.Tasks;
using Streams.StreamTasks;
using Streams.StreamTasks.Internal;
using Streams.StreamTasks.TaskSources;

namespace Streams.UniTasks {

  public static class UniTaskExtensions {

    public static StreamTask ToStreamTask(this UniTask uniTask) {
      if (!TaskSourcePool.TryGet(out UniTaskContinuationSource source))
        source = new UniTaskContinuationSource();

      source.Setup(uniTask);
      StreamTaskHelper.GetRunningStream().AddInvokableTaskSource(source);
      return source.Task;
    }

    public static StreamTask<TResult> ToStreamTask<TResult>(this UniTask<TResult> uniTask) {
      if (!TaskSourcePool.TryGet(out UniTaskContinuationSource<TResult> source))
        source = new UniTaskContinuationSource<TResult>();

      source.Setup(uniTask);
      StreamTaskHelper.GetRunningStream().AddInvokableTaskSource(source);
      return source.Task;
    }

  }

}
#endif