using System;
using System.Collections.Generic;
using NUnit.Framework;
using Unity.PerformanceTesting;
using UnityEngine;

namespace StreamsForUnity.Tests {

  public class PerformanceTests {

    private static readonly bool[] _parallel = { true, false };

    [Test, Performance]
    public void ManyStreamsTest() {
      var sts = new StreamTokenSource();
      var holders = new List<ExecutionStream>(1000);

      for (var i = 0; i < 1000; i++) {
        var stream = new ExecutionStream(sts.Token, $"Stream {i}");
        stream.Add(_ => { });
        holders.Add(stream);
      }

      Measure.Method(UpdateStream)
        .WarmupCount(5)
        .MeasurementCount(60)
        .Run();

      sts.Release();
      return;

      void UpdateStream() {
        foreach (ExecutionStream stream in holders) 
          stream.Update(Time.deltaTime);
      }
    }

    [Test, Performance]
    public void ManyActionsTest([ValueSource(nameof(_parallel))] bool parallel) {
      var sts = new StreamTokenSource();
      var stream = new ExecutionStream(sts.Token, "Stream");

      Action<float> action = _ => {
        var num = 2;
        for (var j = 0; j < 1000; j++)
          num += num;
      };

      for (var i = 0; i < 1000; i++) {
        if (parallel)
          stream.AddParallel(action);
        else
          stream.Add(action);
      }

      Measure.Method(() => stream.Update(Time.deltaTime))
        .WarmupCount(5)
        .MeasurementCount(60)
        .Run();

      sts.Release();
    }

  }

}