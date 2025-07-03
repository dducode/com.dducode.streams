using System;

namespace Streams.StreamTasks.TaskSources {

  internal sealed class DelayedTaskSource : RunnableTaskSource<float> {

    private float _time;

    public override void Setup(float value) {
      _time = value;
    }

    public override void Invoke(float deltaTime) {
      if (IsCompleted)
        return;

      if (CancellationToken.Released) {
        SetCanceled();
        return;
      }

      _time = MathF.Max(0, _time - deltaTime);
      if (_time > 0)
        return;

      SetResult();
    }

    public override void Reset() {
      base.Reset();
      _time = 0;
    }

  }

}