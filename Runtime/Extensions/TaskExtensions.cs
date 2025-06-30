using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using Streams.StreamTasks;
using Streams.StreamTasks.Internal;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Streams.Extensions {

  public static class TaskExtensions {

    public static StreamTask ToStreamTask(this Task task) {
      var streamTask = new StreamTask();
      ExecutionStream runningStream = StreamTaskHelper.GetRunningStream();
      task.ContinueWith(_ => runningStream.ScheduleTaskCompletion(streamTask));
      return streamTask;
    }

    public static StreamTask<TResult> ToStreamTask<TResult>(this Task<TResult> task) {
      var streamTask = new StreamTask<TResult>();
      ExecutionStream runningStream = StreamTaskHelper.GetRunningStream();
      task.ContinueWith(t => runningStream.ScheduleContinuation(() => streamTask.SetResult(t.Result)));
      return streamTask;
    }

    public static StreamTask ToStreamTask(this AsyncOperation asyncOperation) {
      var streamTask = new StreamTask();
      ExecutionStream runningStream = StreamTaskHelper.GetRunningStream();
      asyncOperation.completed += _ => runningStream.ScheduleTaskCompletion(streamTask);
      return streamTask;
    }

    public static StreamTask<Object> ToStreamTask(this ResourceRequest request) {
      var streamTask = new StreamTask<Object>();
      ExecutionStream runningStream = StreamTaskHelper.GetRunningStream();
      request.completed += _ => runningStream.ScheduleContinuation(() => streamTask.SetResult(request.asset));
      return streamTask;
    }

#if STREAMS_UNITASK_INTEGRATION
    public static StreamTask ToStreamTask(this UniTask uniTask) {
      var streamTask = new StreamTask();
      ExecutionStream runningStream = StreamTaskHelper.GetRunningStream();
      uniTask.ContinueWith(() => runningStream.ScheduleTaskCompletion(streamTask));
      return streamTask;
    }

    public static StreamTask<TResult> ToStreamTask<TResult>(this UniTask<TResult> uniTask) {
      var streamTask = new StreamTask<TResult>();
      ExecutionStream runningStream = StreamTaskHelper.GetRunningStream();
      uniTask.ContinueWith(result => runningStream.ScheduleContinuation(() => streamTask.SetResult(result)));
      return streamTask;
    }
#endif

#if UNITY_2023_1_OR_NEWER
    public static StreamTask ToStreamTask(this Awaitable awaitable) {
      var streamTask = new StreamTask();
      ExecutionStream runningStream = StreamTaskHelper.GetRunningStream();
      awaitable.GetAwaiter().OnCompleted(() => runningStream.ScheduleTaskCompletion(streamTask));
      return streamTask;
    }

    public static StreamTask<TResult> ToStreamTask<TResult>(this Awaitable<TResult> awaitable) {
      var streamTask = new StreamTask<TResult>();
      ExecutionStream runningStream = StreamTaskHelper.GetRunningStream();
      awaitable.GetAwaiter().OnCompleted(() => runningStream.ScheduleContinuation(() => streamTask.SetResult(awaitable.GetAwaiter().GetResult())));
      return streamTask;
    }
#endif

  }

}