using System;

namespace Streams.StreamTasks.TaskSources {

  internal sealed class WhenAnyTaskSource : RunnableTaskSource<StreamTask[]> {

    private StreamTask[] _tasks;

    public override void Setup(StreamTask[] value) {
      _tasks = value;
    }

    public override bool Invoke(float deltaTime) {
      for (var i = 0; i < _tasks.Length; i++) {
        StreamTask.Awaiter awaiter = _tasks[i].GetAwaiter();
        if (!awaiter.IsCompleted)
          continue;

        try {
          SetupAwaitersCompletion();
          awaiter.GetResult();
          SetResult();
        }
        catch (OperationCanceledException) {
          SetCanceled();
        }
        catch (Exception e) {
          SetException(e);
        }

        return false;
      }

      return true;
    }

    private void SetupAwaitersCompletion() {
      for (var i = 0; i < _tasks.Length; i++) {
        StreamTask.Awaiter awaiter = _tasks[i].GetAwaiter();
        if (!awaiter.IsCompleted)
          awaiter.OnCompleted(awaiter.GetResult);
      }
    }

    private protected override void Reset() {
      base.Reset();
      _tasks = null;
    }

  }

}