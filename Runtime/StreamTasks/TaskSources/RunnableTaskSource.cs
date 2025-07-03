using Streams.StreamActions;

namespace Streams.StreamTasks.TaskSources {

  public abstract class RunnableTaskSource<TValue> : StreamTaskSource, IInvokable, ICompletable {

    public bool IsCompleted => Status != StreamTaskStatus.Pending;
    public abstract void Setup(TValue value);
    public abstract void Invoke(float deltaTime);

  }

  public abstract class RunnableTaskSource<TValue, TResult> : StreamTaskSource<TResult>, IInvokable, ICompletable {

    public bool IsCompleted => Status != StreamTaskStatus.Pending;
    public abstract void Setup(TValue value);
    public abstract void Invoke(float deltaTime);

  }

}