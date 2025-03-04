namespace StreamsForUnity {

  public interface IUpdatable {

    public uint Priority => uint.MaxValue;
    public void UpdateFunction(float deltaTime);

  }

}