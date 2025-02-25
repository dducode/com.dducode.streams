using System;
using System.Collections.Generic;
using System.Linq;
using StreamsForUnity.StreamHolders.MonoStreamHolders;
using UnityEngine;
using UnityEngine.PlayerLoop;

namespace StreamsForUnity.LODs {

  public abstract class ExecutionLOD<TValue> : MonoBehaviour where TValue : struct {

    [SerializeField] private StreamHolderBase holder;
    [SerializeField] private UpdatableBehaviour[] behaviours;
    [SerializeField, Min(1)] private float maxThreshold = 25;
    [SerializeField] private LODLevel<TValue>[] levels;

    private Dictionary<UpdatableBehaviour, LODItem> _items;

    private void Start() {
      if (levels.Length == 0) {
        Debug.LogWarning("No LOD levels defined");
        return;
      }

      _items = behaviours.ToDictionary(
        behaviour => behaviour,
        behaviour => new LODItem(holder.Stream.Add(behaviour.UpdateFunction, behaviour.destroyCancellationToken))
      );
      UpdateLevels();
      Streams.Get<Update>().Add(_ => UpdateLevels(), destroyCancellationToken);
    }

    private void OnValidate() {
      foreach (LODLevel<TValue> level in levels) {
        MinMaxValue<float> oldValue = level.threshold;
        var newValue = new MinMaxValue<float>(0, maxThreshold) {
          minValue = oldValue.minValue,
          maxValue = oldValue.maxValue
        };
        level.threshold = newValue;
      }
    }

    protected abstract void SetValue(StreamAction action, TValue value);

    private void UpdateLevels() {
      Camera currentCamera = Camera.current ?? Camera.main;
      if (currentCamera == null)
        return;

      foreach ((UpdatableBehaviour behaviour, LODItem item) in _items) {
        while (levels[item.levelIndex].threshold.maxValue < Vector3.Distance(behaviour.transform.position, currentCamera.transform.position)) {
          item.levelIndex = Math.Min(++item.levelIndex, levels.Length - 1);
          SetValue(item.Action, levels[item.levelIndex].value);
        }

        while (levels[item.levelIndex].threshold.minValue > Vector3.Distance(behaviour.transform.position, currentCamera.transform.position)) {
          item.levelIndex = Math.Max(--item.levelIndex, 0);
          SetValue(item.Action, levels[item.levelIndex].value);
        }
      }
    }

  }

}