using System;

namespace StreamsForUnity.LODs {

  [Serializable]
  public sealed class LODLevel<TValue> where TValue : struct {

    public TValue value;
    public MinMaxValue<float> threshold;

  }

}