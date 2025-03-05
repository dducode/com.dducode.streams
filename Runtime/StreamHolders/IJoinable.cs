namespace StreamsForUnity.StreamHolders {

  public interface IJoinable<TConcrete> {

    TConcrete Join(TConcrete other);

  }

}