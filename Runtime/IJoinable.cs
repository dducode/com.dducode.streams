namespace Streams {

  public interface IJoinable<TConcrete> {

    TConcrete Join(TConcrete other);

  }

}