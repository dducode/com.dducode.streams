using Streams.StreamActions;

namespace Streams.StreamTasks.TaskSources {

  internal abstract class RunnableTaskSource<TValue> : StreamTaskSource, IInvokable {

    private protected RunnableTaskSource() {
    }

    public abstract void Setup(TValue value);

    public virtual bool Invoke(float deltaTime) {
      if (CancellationToken.Released) {
        SetCanceled();
        return false;
      }

      return true;
    }

  }

  internal abstract class RunnableTaskSource<TValue, TResult> : StreamTaskSource<TResult>, IInvokable {

    private protected RunnableTaskSource() {
    }

    public abstract void Setup(TValue value);

    public virtual bool Invoke(float deltaTime) {
      if (CancellationToken.Released) {
        SetCanceled();
        return false;
      }

      return true;
    }

  }

}