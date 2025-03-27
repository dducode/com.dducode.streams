using System;
using NUnit.Framework;

namespace Streams.Tests.Attributes {

  [AttributeUsage(AttributeTargets.Method)]
  public class StreamTasksAttribute : CategoryAttribute {

    public StreamTasksAttribute() : base("StreamTasks") {
    }

  }

}