using StreamsForUnity.LODs;
using UnityEditor;

namespace StreamsForUnity.Editor {

  // [CustomEditor(typeof(ExecutionLOD<>), true)]
  public class ExecutionLODEditor : UnityEditor.Editor {

    private SerializedProperty _levels;
    private SerializedProperty[] _thresholdProperties;
    private MinMaxValue<float>[] _cachedThresholds;

    private void OnEnable() {
      _levels = serializedObject.FindProperty("levels");
      UpdatePropertiesArray();
    }

    public override void OnInspectorGUI() {
      DrawDefaultInspector();
      serializedObject.Update();

      if (_levels.arraySize != _thresholdProperties.Length)
        UpdatePropertiesArray();

      for (var i = 0; i < _thresholdProperties.Length - 1; i++) {
        var first = (MinMaxValue<float>)_thresholdProperties[i].boxedValue;
        var second = (MinMaxValue<float>)_thresholdProperties[i + 1].boxedValue;

        if (first.maxValue < second.minValue) {
          if (first.maxValue < _cachedThresholds[i].maxValue) {
            second.minValue = first.maxValue;
            _thresholdProperties[i + 1].boxedValue = second;
          }
          else {
            first.maxValue = second.minValue;
            _thresholdProperties[i].boxedValue = first;
          }
        }
      }

      for (var i = 0; i < _thresholdProperties.Length; i++)
        _cachedThresholds[i] = (MinMaxValue<float>)_thresholdProperties[i].boxedValue;

      serializedObject.ApplyModifiedProperties();
    }

    private void UpdatePropertiesArray() {
      _thresholdProperties = new SerializedProperty[_levels.arraySize];
      _cachedThresholds = new MinMaxValue<float>[_levels.arraySize];

      for (var i = 0; i < _levels.arraySize; i++) {
        SerializedProperty arrayElementAtIndex = _levels.GetArrayElementAtIndex(i);
        SerializedProperty findPropertyRelative = arrayElementAtIndex.FindPropertyRelative("threshold");
        _thresholdProperties[i] = findPropertyRelative;
        _cachedThresholds[i] = (MinMaxValue<float>)_thresholdProperties[i].boxedValue;
      }
    }

  }

}