using System;

namespace Streams.StreamTasks.TaskSources {

  internal sealed class ConditionalTaskSource : RunnableTaskSource<Func<bool>> {

    private Func<bool> _condition;

    public override void Setup(Func<bool> value) {
      _condition = value;
    }

    public override void Invoke(float deltaTime) {
      if (IsCompleted)
        return;

      if (CancellationToken.Released) {
        SetCanceled();
        return;
      }

      if (_condition())
        return;

      SetResult();
    }

    public override void Reset() {
      base.Reset();
      _condition = null;
    }

  }

}