using System;
using System.Collections.Generic;

namespace StreamsForUnity {

  [Serializable]
  public struct MinMaxValue<TValue> where TValue : struct {

    public TValue minLimit;
    public TValue maxLimit;
    public TValue minValue;
    public TValue maxValue;

    public MinMaxValue(TValue minLimit, TValue maxLimit) {
      if (Comparer<TValue>.Default.Compare(minLimit, maxLimit) > 0)
        throw new InvalidOperationException("a minLimit value cannot be more than a maxLimit value");

      this.minLimit = minLimit;
      this.maxLimit = maxLimit;
      minValue = minLimit;
      maxValue = maxLimit;
    }

  }

}