namespace Streams.StreamTasks.TaskSources {

  internal sealed class AnyoneWaitingTaskSource : RunnableTaskSource<StreamTask[]> {

    private StreamTask[] _tasks;

    public override void Setup(StreamTask[] value) {
      _tasks = value;
    }

    public override void Invoke(float deltaTime) {
      if (IsCompleted)
        return;

      if (CancellationToken.Released) {
        SetCanceled();
        return;
      }

      var any = false;
      for (var i = 0; i < _tasks.Length; i++)
        any |= _tasks[i].GetAwaiter().IsCompleted;

      if (!any)
        return;

      SetResult();
    }

    public override void Reset() {
      base.Reset();
      _tasks = null;
    }

  }

}