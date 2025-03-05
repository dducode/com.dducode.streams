using System;
using JetBrains.Annotations;

namespace StreamsForUnity.Attributes {

  [AttributeUsage(AttributeTargets.Method), MeansImplicitUse(ImplicitUseKindFlags.Access)]
  public class PersistentUpdateAttribute : Attribute {

    public uint Priority { get; }

    public PersistentUpdateAttribute(uint priority = uint.MaxValue) {
      Priority = priority;
    }

  }

}