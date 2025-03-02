namespace StreamsForUnity.StreamHolders.MonoStreamHolders {

  public interface IJoinable<TConcrete> {

    TConcrete Join(TConcrete other);

  }

}