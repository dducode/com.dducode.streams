using System;

namespace Streams.StreamTasks.TaskSources {

  internal sealed class DelayedTaskSource : RunnableTaskSource<float> {

    private float _time;

    public override void Setup(float value) {
      _time = value;
    }

    public override bool Invoke(float deltaTime) {
      if (!base.Invoke(deltaTime))
        return false;

      _time = MathF.Max(0, _time - deltaTime);
      if (_time > 0)
        return true;

      SetResult();
      return false;
    }

    private protected override void Reset() {
      base.Reset();
      _time = 0;
    }

  }

}