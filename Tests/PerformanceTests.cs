using System;
using NUnit.Framework;
using Unity.PerformanceTesting;
using UnityEngine;
using Random = System.Random;

namespace StreamsForUnity.Tests {

  public class PerformanceTests {

    public enum ExecutionType {

      Sequential,
      Parallel

    }

    private static readonly ExecutionType[] _executionType = { ExecutionType.Sequential, ExecutionType.Parallel };

    private static readonly ParallelWorkStrategy[] _parallelWorkStrategies = {
      ParallelWorkStrategy.Economy,
      ParallelWorkStrategy.Effectively,
      ParallelWorkStrategy.Performance
    };

    [Test, Performance]
    public void ManyStreamsTest() {
      var sts = new StreamTokenSource();
      var baseStream = new ExecutionStream("base");
      sts.Register(baseStream.Dispose_Internal);

      for (var i = 0; i < 1000; i++) {
        var stream = new ExecutionStream($"Stream {i}");
        sts.Register(stream.Dispose_Internal);
        baseStream.Add(stream.Update);
        stream.Add(_ => { });
      }

      Measure.Method(() => baseStream.Update(Time.deltaTime))
        .WarmupCount(5)
        .MeasurementCount(60)
        .Run();

      sts.Release();
    }

    [Test, Performance]
    public void ManyActionsTest([ValueSource(nameof(_executionType))] ExecutionType executionType) {
      var sts = new StreamTokenSource();
      var stream = new ExecutionStream("Stream") {
        ParallelWorkStrategy = ParallelWorkStrategy.Performance
      };
      sts.Register(stream.Dispose_Internal);

      Action<float> work = _ => {
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

      sts.Release();
    }

    [Test, Performance]
    public void ParallelWorkStrategyTest([ValueSource(nameof(_parallelWorkStrategies))] ParallelWorkStrategy strategy) {
      var sts = new StreamTokenSource();
      var stream = new ExecutionStream("Stream") {
        ParallelWorkStrategy = strategy
      };
      sts.Register(stream.Dispose_Internal);

      Action<float> work = _ => {
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

      sts.Release();
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