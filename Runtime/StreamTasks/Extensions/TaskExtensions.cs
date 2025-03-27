using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using Streams.StreamTasks.Internal;
using UnityEngine;

namespace Streams.StreamTasks.Extensions {

  public static class TaskExtensions {

    public static StreamTask ToStreamTask(this Task task) {
      var streamTask = new StreamTask();
      ExecutionStream runningStream = StreamTaskHelper.GetRunningStream();
      task.ContinueWith(_ => runningStream.AddOnce(streamTask.SetResult));
      return streamTask;
    }

#if STREAMS_UNITASK_INTEGRATION
    public static StreamTask ToStreamTask(this UniTask uniTask) {
      var streamTask = new StreamTask();
      ExecutionStream runningStream = StreamTaskHelper.GetRunningStream();
      uniTask.ContinueWith(() => runningStream.AddOnce(streamTask.SetResult));
      return streamTask;
    }
#endif

#if UNITY_2023_1_OR_NEWER
    public static StreamTask ToStreamTask(this Awaitable awaitable) {
      var streamTask = new StreamTask();
      ExecutionStream runningStream = StreamTaskHelper.GetRunningStream();
      awaitable.GetAwaiter().OnCompleted(() => runningStream.AddOnce(streamTask.SetResult));
      return streamTask;
    }
#endif

  }

}