using System;
using JetBrains.Annotations;

namespace StreamsForUnity.Attributes {

  [AttributeUsage(AttributeTargets.Method), MeansImplicitUse(ImplicitUseKindFlags.Access)]
  public class ParallelUpdateAttribute : Attribute {

  }

}