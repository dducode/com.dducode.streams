using System;
using UnityEngine.LowLevel;
using UnityEngine.PlayerLoop;

namespace Streams.Internal {

  internal static class SystemIdentifier {

    internal static bool IsFixedSystem(Type systemType) {
      var isFixed = false;
      PlayerLoopSystem system = PlayerLoop.GetCurrentPlayerLoop();
      IsFixedSystem(systemType, ref isFixed, ref system);
      return isFixed;
    }

    /// <returns>Return value used as a flag that the target system was found</returns>
    private static bool IsFixedSystem(Type targetSystemType, ref bool isFixed, ref PlayerLoopSystem system) {
      if (system.type == typeof(FixedUpdate))
        isFixed = true;

      if (system.type == targetSystemType)
        return true;

      if (system.subSystemList == null)
        return false;

      for (var i = 0; i < system.subSystemList.Length; i++)
        if (IsFixedSystem(targetSystemType, ref isFixed, ref system.subSystemList[i]))
          return true;

      if (system.type == typeof(FixedUpdate))
        isFixed = false;

      return false;
    }

  }

}