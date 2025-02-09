using System;
using System.Text;
using JetBrains.Annotations;

namespace StreamsForUnity.Internal {

  internal static class NamesUtility {

    internal static string CreateProfilerSampleName([NotNull] Type systemType) {
      if (systemType == null)
        throw new ArgumentNullException(nameof(systemType));
      var builder = new StringBuilder(systemType.Name);
      Type type = systemType.DeclaringType;

      while (type != null) {
        builder.Insert(0, $"{type.Name}.");
        type = type.DeclaringType;
      }

      return builder.ToString();
    }

  }

}