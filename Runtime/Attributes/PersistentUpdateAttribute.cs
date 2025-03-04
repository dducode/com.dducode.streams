using System;

namespace StreamsForUnity.Attributes {

  [AttributeUsage(AttributeTargets.Method)]
  public class PersistentUpdateAttribute : Attribute {

    public uint Priority { get; }

    public PersistentUpdateAttribute(uint priority = uint.MaxValue) {
      Priority = priority;
    }

  }

}