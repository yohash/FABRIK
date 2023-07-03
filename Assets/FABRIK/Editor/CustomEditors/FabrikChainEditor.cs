using UnityEditor;
using UnityEngine;

namespace Yohash.FABRIK
{
  [CustomEditor(typeof(FabrikChain))]
  [CanEditMultipleObjects]
  public class FabrikChainEditor : Editor
  {
    private FabrikChain script;
    private GameObject scriptObject;

    private SerializedProperty positions;

    private bool showSolution;
    private FabrikChainSolutionGizmo _solutionGizmo;

    protected GUIStyle Style {
      get {
        var style = EditorStyles.label;
        return style;
      }
    }

    protected virtual void OnEnable()
    {
      // save local references
      script = (FabrikChain)target;
      scriptObject = script.gameObject;

      positions = serializedObject.FindProperty("Positions");
    }

    public override void OnInspectorGUI()
    {
      base.OnInspectorGUI();
      serializedObject.Update();

      EditorGUILayout.LabelField("Debug", Style.Bold());

      showSolution = scriptObject.GetComponent<FabrikChainSolutionGizmo>() != null;
      showSolution = EditorGUILayout.Toggle("Show Solution", showSolution);

      drawLineGizmo();

      serializedObject.ApplyModifiedProperties();
    }

    private void drawLineGizmo()
    {
      if (showSolution) {
        if (_solutionGizmo == null) {
          _solutionGizmo = scriptObject.GetComponent<FabrikChainSolutionGizmo>();
        }
        if (_solutionGizmo == null) {
          _solutionGizmo = scriptObject.AddComponent<FabrikChainSolutionGizmo>();
        }

        var solutions = new Vector3[positions.arraySize];
        for (int i = 0; i < positions.arraySize; i++) {
          solutions[i] = positions.GetArrayElementAtIndex(i).vector3Value;
        }

        _solutionGizmo.Solution = solutions;
      } else {
        if (_solutionGizmo != null) {
          DestroyImmediate(_solutionGizmo);
        }
      }
    }
  }
}