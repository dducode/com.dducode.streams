using System.Threading.Tasks;
using UnityEngine;

#if STREAMS_UNITASK_INTEGRATION
using Cysharp.Threading.Tasks;
#endif

namespace StreamsForUnity.StreamTasks.Extensions {

  public static class TaskExtensions {

    public static StreamTask ToStreamTask(this Task task) {
      var streamTask = new StreamTask();
      task.ContinueWith(_ => streamTask.SetResult());
      return streamTask;
    }

#if STREAMS_UNITASK_INTEGRATION
    public static StreamTask ToStreamTask(this UniTask uniTask) {
      var streamTask = new StreamTask();
      uniTask.ContinueWith(streamTask.SetResult);
      return streamTask;
    }
#endif

#if UNITY_2023_1_OR_NEWER
    public static async StreamTask ToStreamTask(this Awaitable awaitable) {
      await awaitable;
    }
#endif

  }

}