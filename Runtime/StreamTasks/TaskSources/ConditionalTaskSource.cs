using System;

namespace Streams.StreamTasks.TaskSources {

  internal sealed class ConditionalTaskSource : RunnableTaskSource<Func<bool>> {

    private Func<bool> _condition;

    public override void Setup(Func<bool> value) {
      _condition = value;
    }

    public override bool Invoke(float deltaTime) {
      if (!base.Invoke(deltaTime))
        return false;

      if (_condition())
        return true;

      SetResult();
      return false;
    }

    private protected override void Reset() {
      base.Reset();
      _condition = null;
    }

  }

}