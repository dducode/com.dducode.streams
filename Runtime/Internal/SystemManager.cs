using System;
using StreamsForUnity.Exceptions;
using UnityEngine.LowLevel;

namespace StreamsForUnity.Internal {

  internal static class SystemManager {

    internal static void SetupSystem<TBaseSystem>(PlayerLoopSystem newSystem) {
      SetupSystem(typeof(TBaseSystem), newSystem);
    }

    internal static void SetupSystem(Type baseSystemType, PlayerLoopSystem newSystem) {
      PlayerLoopSystem system = PlayerLoop.GetCurrentPlayerLoop();
      if (!FindSystemAndSetup(baseSystemType, ref system, newSystem))
        throw new StreamsException("Cannot setup system because base system doesn't exist");
      PlayerLoop.SetPlayerLoop(system);
    }

    internal static void RemoveSystem<TBaseSystem, TSystem>() {
      PlayerLoopSystem system = PlayerLoop.GetCurrentPlayerLoop();
      if (!FindSystemAndRemove<TSystem>(typeof(TBaseSystem), ref system))
        throw new StreamsException("Cannot remove system because base system doesn't exist");
      PlayerLoop.SetPlayerLoop(system);
    }

    private static bool FindSystemAndSetup(Type baseSystemType, ref PlayerLoopSystem system, PlayerLoopSystem newSystem) {
      if (system.type == baseSystemType) {
        InsertSystemAtLast(ref system, newSystem);
        return true;
      }

      if (system.subSystemList == null)
        return false;

      for (var i = 0; i < system.subSystemList.Length; i++)
        if (FindSystemAndSetup(baseSystemType, ref system.subSystemList[i], newSystem))
          return true;

      return false;
    }

    private static bool FindSystemAndRemove<TSystem>(Type baseSystemType, ref PlayerLoopSystem system) {
      if (system.type == baseSystemType) {
        RemoveSystem<TSystem>(ref system);
        return true;
      }

      if (system.subSystemList == null)
        return false;

      for (var i = 0; i < system.subSystemList.Length; i++)
        if (FindSystemAndRemove<TSystem>(baseSystemType, ref system.subSystemList[i]))
          return true;

      return false;
    }

    private static void InsertSystemAtLast(ref PlayerLoopSystem system, PlayerLoopSystem newSystem) {
      if (system.subSystemList == null) {
        system.subSystemList = new[] { newSystem };
        return;
      }

      var newSubSystems = new PlayerLoopSystem[system.subSystemList.Length + 1];

      for (var i = 0; i < newSubSystems.Length - 1; i++)
        newSubSystems[i] = system.subSystemList[i];

      newSubSystems[^1] = newSystem;
      system.subSystemList = newSubSystems;
    }

    private static void RemoveSystem<TSystem>(ref PlayerLoopSystem system) {
      var newSubSystems = new PlayerLoopSystem[system.subSystemList.Length - 1];

      var i = 0;

      foreach (PlayerLoopSystem subSystem in system.subSystemList) {
        if (subSystem.type == typeof(TSystem))
          continue;
        newSubSystems[i++] = subSystem;
      }

      system.subSystemList = newSubSystems;
    }

  }

}