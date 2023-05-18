using UnityEditor;
using UnityEngine;

namespace Yohash.FABRIK
{
  [CustomEditor(typeof(PhysicalFabrikJoint))]
  [CanEditMultipleObjects]
  public class PhysicalFabrikJointEditor : FabrikJointEditor
  {
    // *** Rotation motor force
    private SerializedProperty applyRotationForce;
    private SerializedProperty rotator;
    private SerializedProperty torque;

    // *** Translational motor force
    private SerializedProperty applyTranslationForce;
    private SerializedProperty matchVelocity;
    private SerializedProperty translator;
    private SerializedProperty throttle;

    protected override void OnEnable()
    {
      applyRotationForce = serializedObject.FindProperty("applyRotationForce");
      rotator = serializedObject.FindProperty("rotator");
      torque = serializedObject.FindProperty("outputTorque");

      applyTranslationForce = serializedObject.FindProperty("applyTranslationForce");
      matchVelocity = serializedObject.FindProperty("matchVelocity");
      translator = serializedObject.FindProperty("translator");
      throttle = serializedObject.FindProperty("outputThrottle");

      base.OnEnable();
    }

    private void HorizontalLine(Color color)
    {
      GUIStyle horizontalLine;
      horizontalLine = new GUIStyle();
      horizontalLine.normal.background = EditorGUIUtility.whiteTexture;
      horizontalLine.margin = new RectOffset(0, 0, 4, 4);
      horizontalLine.fixedHeight = 1;

      var c = GUI.color;
      GUI.color = color;
      GUILayout.Box(GUIContent.none, horizontalLine);
      GUI.color = c;
    }

    public override void OnInspectorGUI()
    {
      serializedObject.Update();

      EditorGUILayout.LabelField("Physical Fabrik Joint", Style.Bold());
      //HorizontalLine(new Color(0.8f, 0.8f, 0.8f, 0.5f));
      EditorGUILayout.LabelField("");

      EditorGUILayout.PropertyField(applyRotationForce);
      if (applyRotationForce.boolValue) {
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("", GUILayout.MaxWidth(40), GUILayout.ExpandWidth(false));
        EditorGUILayout.PropertyField(rotator);
        EditorGUILayout.EndHorizontal();

        var c = GUI.color;
        GUI.color = new Color(0.8f, 0.8f, 0.8f, 0.5f);
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("", GUILayout.MaxWidth(40), GUILayout.ExpandWidth(false));
        EditorGUILayout.PropertyField(torque);
        EditorGUILayout.EndHorizontal();
        GUI.color = c;

        EditorGUILayout.LabelField("");
      }

      EditorGUILayout.PropertyField(applyTranslationForce);
      if (applyTranslationForce.boolValue) {
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("", GUILayout.MaxWidth(40), GUILayout.ExpandWidth(false));
        EditorGUILayout.PropertyField(matchVelocity);
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("", GUILayout.MaxWidth(40), GUILayout.ExpandWidth(false));
        EditorGUILayout.PropertyField(translator);
        EditorGUILayout.EndHorizontal();

        var c = GUI.color;
        GUI.color = new Color(0.8f, 0.8f, 0.8f, 0.5f);
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("", GUILayout.MaxWidth(40), GUILayout.ExpandWidth(false));
        EditorGUILayout.PropertyField(throttle);
        EditorGUILayout.EndHorizontal();
        GUI.color = c;

        //EditorGUILayout.LabelField("");
      }

      serializedObject.ApplyModifiedProperties();

      EditorGUILayout.LabelField("");
      HorizontalLine(new Color(0.8f, 0.8f, 0.8f, 0.5f));
      EditorGUILayout.LabelField("Standard Fabrik Joint", Style.Bold());
      //HorizontalLine(new Color(0.8f, 0.8f, 0.8f, 0.5f));
      EditorGUILayout.LabelField("");

      base.OnInspectorGUI();
    }
  }
}