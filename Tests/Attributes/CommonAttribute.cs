using System;
using NUnit.Framework;

namespace StreamsForUnity.Tests.Attributes {

  [AttributeUsage(AttributeTargets.Method)]
  public class CommonAttribute : CategoryAttribute {

    public CommonAttribute() : base("Common") {
    }

  }

}