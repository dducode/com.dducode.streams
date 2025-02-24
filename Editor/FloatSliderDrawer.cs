using System;
using UnityEditor;
using UnityEngine;

namespace StreamsForUnity.Editor {

  [CustomPropertyDrawer(typeof(MinMaxValue<float>))]
  public class FloatSliderDrawer : PropertyDrawer {

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {
      var value = (MinMaxValue<float>)property.boxedValue;
      float minValue = value.minValue;
      float maxValue = value.maxValue;
      EditorGUI.MinMaxSlider(position, label, ref minValue, ref maxValue, value.minLimit, value.maxLimit);

      Rect rect = EditorGUILayout.GetControlRect();
      position.width = (rect.width - EditorGUIUtility.labelWidth) / 2;
      position.x += EditorGUIUtility.labelWidth;
      position.y += position.height;

      minValue = Math.Max(EditorGUI.FloatField(position, minValue), value.minLimit);
      position.x += position.width;
      float maxFloat = EditorGUI.FloatField(position, maxValue);
      maxValue = maxFloat < float.PositiveInfinity ? Math.Min(maxFloat, value.maxLimit) : maxFloat;

      value.minValue = minValue;
      value.maxValue = maxValue;
      property.boxedValue = value;
    }

  }

}