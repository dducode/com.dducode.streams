using System.Threading.Tasks;
using StreamsForUnity.StreamTasks.Internal;
using UnityEngine;

#if STREAMS_UNITASK_INTEGRATION
using Cysharp.Threading.Tasks;
#endif

namespace StreamsForUnity.StreamTasks.Extensions {

  public static class TaskExtensions {

    public static StreamTask ToStreamTask(this Task task) {
      var streamTask = new StreamTask();
      IExecutionStream runningStream = StreamTaskHelper.GetRunningStream();
      task.ContinueWith(_ => runningStream.AddOnce(streamTask.SetResult));
      return streamTask;
    }

#if STREAMS_UNITASK_INTEGRATION
    public static StreamTask ToStreamTask(this UniTask uniTask) {
      var streamTask = new StreamTask();
      IExecutionStream runningStream = StreamTaskHelper.GetRunningStream();
      uniTask.ContinueWith(() => runningStream.AddOnce(streamTask.SetResult));
      return streamTask;
    }
#endif

#if UNITY_2023_1_OR_NEWER
    public static StreamTask ToStreamTask(this Awaitable awaitable) {
      var streamTask = new StreamTask();
      IExecutionStream runningStream = StreamTaskHelper.GetRunningStream();
      awaitable.GetAwaiter().OnCompleted(() => runningStream.AddOnce(streamTask.SetResult));
      return streamTask;
    }
#endif

  }

}