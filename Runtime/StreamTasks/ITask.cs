namespace Streams.StreamTasks {

  internal interface ITask {

    public bool IsCompleted { get; }
    internal void SetCanceled();

  }

}