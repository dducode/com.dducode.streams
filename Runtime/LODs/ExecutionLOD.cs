using System;
using StreamsForUnity.StreamHolders;
using UnityEngine;
using UnityEngine.PlayerLoop;

namespace StreamsForUnity.LODs {

  public abstract class ExecutionLOD<TValue> : MonoBehaviour where TValue : struct {

    [SerializeField] protected StreamHolderBase holder;
    [SerializeField, Min(1)] private float maxThreshold = 25;
    [SerializeField] private LODLevel<TValue>[] levels;

    private int _currentLevel;

    private void Start() {
      if (levels.Length == 0) {
        Debug.LogWarning("No LOD levels defined");
        return;
      }

      Streams.Get<Update>().Add(_ => UpdateLevels(), destroyCancellationToken);
    }

    private void OnValidate() {
      if (levels == null)
        return;

      foreach (LODLevel<TValue> level in levels) {
        MinMaxValue<float> oldValue = level.threshold;
        var newValue = new MinMaxValue<float>(0, maxThreshold) {
          minValue = oldValue.minValue,
          maxValue = oldValue.maxValue
        };
        level.threshold = newValue;
      }
    }

    protected abstract void SetValue(TValue value);

    private void UpdateLevels() {
      Camera currentCamera = Camera.current ?? Camera.main;
      if (currentCamera == null)
        return;

      if (levels[_currentLevel].threshold.maxValue < Vector3.Distance(holder.transform.position, currentCamera.transform.position)) {
        _currentLevel = Math.Min(++_currentLevel, levels.Length - 1);
        SetValue(levels[_currentLevel].value);
      }

      if (levels[_currentLevel].threshold.minValue > Vector3.Distance(holder.transform.position, currentCamera.transform.position)) {
        _currentLevel = Math.Max(--_currentLevel, 0);
        SetValue(levels[_currentLevel].value);
      }
    }

  }

}