using System;
using System.Threading;
using NUnit.Framework;
using Streams.StreamActions;
using Unity.PerformanceTesting;
using UnityEngine;
using Random = System.Random;

namespace Streams.Tests {

  public class PerformanceTests {

    public enum ExecutionType {

      Sequential,
      Parallel

    }

    private static readonly ExecutionType[] _executionType = { ExecutionType.Sequential, ExecutionType.Parallel };

    private static readonly ParallelWorkStrategy[] _parallelWorkStrategies = {
      ParallelWorkStrategy.Economy,
      ParallelWorkStrategy.Optimal,
      ParallelWorkStrategy.Performance
    };

    [Test, Performance]
    public void ManyStreamsTest() {
      var sts = new CancellationTokenSource();
      var baseStream = new ExecutionStream("base");
      sts.Token.Register(baseStream.Terminate);

      for (var i = 0; i < 1000; i++) {
        var stream = new ExecutionStream($"Stream {i}");
        sts.Token.Register(stream.Terminate);
        baseStream.Add(self => stream.Update(self.DeltaTime));
        stream.Add(_ => { });
      }

      Measure.Method(() => baseStream.Update(Time.deltaTime))
        .WarmupCount(5)
        .MeasurementCount(60)
        .Run();

      sts.Cancel();
    }

    [Test, Performance]
    public void ManyActionsTest([ValueSource(nameof(_executionType))] ExecutionType executionType) {
      var sts = new CancellationTokenSource();
      var stream = new ExecutionStream("Stream") {
        WorkStrategy = ParallelWorkStrategy.Performance
      };
      sts.Token.Register(stream.Terminate);

      Action<SelfClosingAction> work = _ => {
        Matrix4x4 matrix = GetRandomMatrix();
        for (var j = 0; j < 1000; j++)
          matrix *= matrix;
      };

      for (var i = 0; i < 100; i++) {
        switch (executionType) {
          case ExecutionType.Parallel:
            stream.AddParallel(work);
            break;
          case ExecutionType.Sequential:
            stream.Add(work);
            break;
          default:
            throw new ArgumentOutOfRangeException(nameof(executionType), executionType, null);
        }
      }

      Measure.Method(() => stream.Update(Time.deltaTime))
        .WarmupCount(5)
        .MeasurementCount(60)
        .Run();

      sts.Cancel();
    }

    [Test, Performance]
    public void ParallelWorkStrategyTest([ValueSource(nameof(_parallelWorkStrategies))] ParallelWorkStrategy strategy) {
      var sts = new CancellationTokenSource();
      var stream = new ExecutionStream("Stream") {
        WorkStrategy = strategy
      };
      sts.Token.Register(stream.Terminate);

      Action<SelfClosingAction> work = _ => {
        Matrix4x4 matrix = GetRandomMatrix();
        for (var j = 0; j < 1000; j++)
          matrix *= matrix;
      };

      for (var i = 0; i < 100; i++)
        stream.AddParallel(work);

      Measure.Method(() => stream.Update(Time.deltaTime))
        .WarmupCount(5)
        .MeasurementCount(60)
        .Run();

      sts.Cancel();
    }

    private static readonly Random _random = new();

    private static Matrix4x4 GetRandomMatrix() {
      return new Matrix4x4(
        GetRandomVector4(),
        GetRandomVector4(),
        GetRandomVector4(),
        GetRandomVector4()
      );
    }

    private static Vector4 GetRandomVector4() {
      return new Vector4(_random.Next(0, 1), _random.Next(0, 1), _random.Next(0, 1), _random.Next(0, 1));
    }

  }

}