using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using StreamsForUnity.Attributes;
using StreamsForUnity.StreamHolders;
using UnityEngine;

namespace StreamsForUnity.Internal {

  internal static class BehavioursHelper {

    internal static void SearchBehaviours(HashSet<MonoBehaviour> hashSet, Transform target) {
      IEnumerable<MonoBehaviour> behaviours = target
        .GetComponents<MonoBehaviour>()
        .Where(component => component.GetType().IsDefined(typeof(DynamicStreamBindAttribute)));

      foreach (MonoBehaviour behaviour in behaviours)
        hashSet.Add(behaviour);

      for (var i = 0; i < target.childCount; i++) {
        Transform child = target.GetChild(i);
        if (child != null && child.GetComponent<IStreamHolder>() == null)
          SearchBehaviours(hashSet, child);
      }
    }

    internal static void ConnectBehavioursToStream(ExecutionStream stream, HashSet<MonoBehaviour> behaviours) {
      foreach (MonoBehaviour behaviour in behaviours) {
        MethodInfo[] methods = behaviour
          .GetType()
          .GetMethods(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);

        AddPersistentActions(stream, behaviour, methods);
        AddParallelActions(stream, behaviour, methods);
      }
    }

    private static void AddPersistentActions(ExecutionStream stream, MonoBehaviour behaviour, MethodInfo[] methods) {
      if (behaviour is IUpdatable updatable)
        stream.Add(updatable.UpdateFunction, behaviour.destroyCancellationToken, updatable.Priority);

      IEnumerable<MethodInfo> persistentMethods = methods.Where(method => method.IsDefined(typeof(PersistentUpdateAttribute)));
      foreach (MethodInfo method in persistentMethods) {
        try {
          var action = (Action<float>)Delegate.CreateDelegate(typeof(Action<float>), behaviour, method);
          var attribute = method.GetCustomAttribute<PersistentUpdateAttribute>();
          stream.Add(action, behaviour.destroyCancellationToken, attribute.Priority);
        }
        catch (TargetParameterCountException) {
          Debug.LogError($"Method {method} has an invalid parameters count", behaviour);
        }
        catch (ArgumentException) {
          Debug.LogError($"Method {method} has an invalid signature", behaviour);
        }
      }
    }

    private static void AddParallelActions(ExecutionStream stream, MonoBehaviour behaviour, MethodInfo[] methods) {
      if (behaviour is IParallelUpdatable parallelUpdatable)
        stream.AddParallel(parallelUpdatable.ParallelUpdate, behaviour.destroyCancellationToken);

      IEnumerable<MethodInfo> parallelMethods = methods.Where(method => method.IsDefined(typeof(ParallelUpdateAttribute)));
      foreach (MethodInfo method in parallelMethods) {
        try {
          var action = (Action<float>)Delegate.CreateDelegate(typeof(Action<float>), behaviour, method);
          stream.AddParallel(action, behaviour.destroyCancellationToken);
        }
        catch (TargetParameterCountException) {
          Debug.LogError($"Method {method} has an invalid parameters count", behaviour);
        }
        catch (ArgumentException) {
          Debug.LogError($"Method {method} has an invalid signature", behaviour);
        }
      }
    }

  }

}