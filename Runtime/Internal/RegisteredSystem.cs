using System;
using System.Collections.Generic;

namespace Streams.Internal {

  internal class RegisteredSystem {

    public RegisteredSystem BaseSystem { get; }
    public Type SystemType { get; }
    public Dictionary<Type, RegisteredSystem> SubSystems { get; } = new();

    internal RegisteredSystem(RegisteredSystem baseSystem, Type systemType) {
      BaseSystem = baseSystem;
      SystemType = systemType;
    }

    public bool TryGetRegisteredSystem(Type systemType, out RegisteredSystem system) {
      return TryGetRegisteredSystem(systemType, this, out system);
    }

    public void Clear() {
      foreach (RegisteredSystem subSystem in SubSystems.Values)
        subSystem.Clear();
      SubSystems.Clear();
    }

    private bool TryGetRegisteredSystem(Type systemType, RegisteredSystem baseSystem, out RegisteredSystem system) {
      if (baseSystem.SubSystems.TryGetValue(systemType, out system))
        return true;

      if (baseSystem.SubSystems.Count > 0)
        foreach (RegisteredSystem registeredSystem in baseSystem.SubSystems.Values)
          if (TryGetRegisteredSystem(systemType, registeredSystem, out system))
            return true;

      system = null;
      return false;
    }

  }

}