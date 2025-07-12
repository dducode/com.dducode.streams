#if STREAMS_UNITASK_INTEGRATION
using Cysharp.Threading.Tasks;
using Streams.StreamTasks;
using Streams.StreamTasks.Internal;

namespace Streams.Extensions {

  public static class UniTaskExtensions {

    public static StreamTask ToStreamTask(this UniTask uniTask) {
      var source = Pool.Get<UniTaskContinuationSource>();
      source.Setup(uniTask);
      StreamTaskHelper.GetRunningStream().AddInvokableTaskSource(source);
      return source.Task;
    }

    public static StreamTask<TResult> ToStreamTask<TResult>(this UniTask<TResult> uniTask) {
      var source = Pool.Get<UniTaskContinuationSource<TResult>>();
      source.Setup(uniTask);
      StreamTaskHelper.GetRunningStream().AddInvokableTaskSource(source);
      return source.Task;
    }

  }

}
#endif