namespace Streams.StreamTasks.TaskSources {

  internal class DelayFrameTaskSource : RunnableTaskSource<int> {

    private int _frames;

    public override void Setup(int value) {
      _frames = value;
    }

    public override bool Invoke(float deltaTime) {
      if (!base.Invoke(deltaTime))
        return false;

      if (--_frames > 0)
        return true;

      SetResult();
      return false;
    }

  }

}