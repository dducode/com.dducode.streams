namespace Streams.StreamActions {

  public interface IConfigurable<out TConcrete> {

    public TConcrete SetDelta(float value);
    public TConcrete ResetDelta();
    public TConcrete SetTickRate(uint value);

  }

}