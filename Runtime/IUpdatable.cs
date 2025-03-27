namespace Streams {

  public interface IUpdatable {

    public void Initialize() {
    }

    public void UpdateFunction(float deltaTime);

    public void Shutdown() {
    }

  }

}