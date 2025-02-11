using System;
using NUnit.Framework;

namespace StreamsForUnity.Tests.Attributes {

  [AttributeUsage(AttributeTargets.Method)]
  public class StreamTasksAttribute : CategoryAttribute {

    public StreamTasksAttribute() : base("StreamTasks") {
    }

  }

}