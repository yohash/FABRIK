using UnityEditor;
using UnityEngine;

namespace Yohash.FABRIK
{
  [CustomEditor(typeof(FabrikJoint))]
  [CanEditMultipleObjects]
  public class FabrikJointEditor : Editor
  {
    private FabrikJoint script;
    private GameObject scriptObject;

    // *** Joint Weight
    private SerializedProperty jointWeight;
    private float jointWeightValue;

    // *** Constrain Rotation
    private SerializedProperty constrainRotation;

    // assuming +z as the "forward", these are
    private float rightLimit = 60;    // +x
    private float leftLimit = -60;    // -x
    private float upLimit = 60;       // +y
    private float downLimit = -60;    // -y

    private SerializedProperty roteRight;
    private SerializedProperty roteLeft;
    private SerializedProperty roteUp;
    private SerializedProperty roteDown;

    private bool showCone = false;
    private ConeGizmo _coneGizmo;

    // *** Preferred Forward direction
    private SerializedProperty hasPreferredDirection;
    private SerializedProperty preferredDirection;
    private SerializedProperty preferredDirectionStrength;
    private float preferredDirectionStrengthValue;

    private bool showPreferredDirection = false;
    private LineGizmo _lineGizmo;

    // *** Preferred Up Facing
    private SerializedProperty hasPreferredUp;
    private SerializedProperty lookAtUpOverride;
    private SerializedProperty preferenceTowardsUpchain;
    private float preferenceTowardsUpchainValue;

    private bool showDesiredUp = false;
    private MatchingArrowGizmo _ArrowGizmo;

    // ** Other display values
    private SerializedProperty upchain;
    private SerializedProperty downchain;
    private SerializedProperty downstreamDistance;
    private SerializedProperty upstreamDistance;

    private GUIStyle Style {
      get {
        var style = EditorStyles.label;
        return style;
      }
    }

    private void OnEnable()
    {
      // *** Joint Weight
      jointWeight = serializedObject.FindProperty("jointWeight");
      jointWeightValue = jointWeight.floatValue;

      // *** Constrain Rotation
      constrainRotation = serializedObject.FindProperty("constrainRotation");
      roteRight = serializedObject.FindProperty("roteRight");
      roteLeft = serializedObject.FindProperty("roteLeft");
      roteUp = serializedObject.FindProperty("roteUp");
      roteDown = serializedObject.FindProperty("roteDown");

      rightLimit = roteRight.floatValue;
      leftLimit = roteLeft.floatValue;
      upLimit = roteUp.floatValue;
      downLimit = roteDown.floatValue;

      // *** Preferred Forward direction
      hasPreferredDirection = serializedObject.FindProperty("hasPreferredDirection");
      preferredDirection = serializedObject.FindProperty("preferredRelativeForward");
      preferredDirectionStrength = serializedObject.FindProperty("preferredDirectionStrength");
      preferredDirectionStrengthValue = preferredDirectionStrength.floatValue;

      // *** Preferred Up Facing
      hasPreferredUp = serializedObject.FindProperty("hasPreferredUp");
      lookAtUpOverride = serializedObject.FindProperty("lookAtUpOverride");
      preferenceTowardsUpchain = serializedObject.FindProperty("preferenceTowardsUpchain");
      preferenceTowardsUpchainValue = preferenceTowardsUpchain.floatValue;

      // *** Misc.
      upchain = serializedObject.FindProperty("upchain");
      downchain = serializedObject.FindProperty("downchain");
      downstreamDistance = serializedObject.FindProperty("downstreamDistance");
      upstreamDistance = serializedObject.FindProperty("upstreamDistance");


      // save local references
      script = (FabrikJoint)target;
      scriptObject = script.gameObject;
    }

    public override void OnInspectorGUI()
    {
      serializedObject.Update();

      // *** Joint Weight
      defineJointWeight();

      // *** Constrain Rotation
      EditorGUILayout.LabelField("Define Conic Rotational Constraints", Style.Bold());
      if (constrainRotation.boolValue = GUILayout.Toggle(constrainRotation.boolValue, "  Constrain Rotation")) {
        drawConstrainRotation();
      }

      // *** Preferred Forward direction
      EditorGUILayout.LabelField("Define Preferred Direction", Style.Bold());
      if (hasPreferredDirection.boolValue = GUILayout.Toggle(hasPreferredDirection.boolValue, "  Has Preferred Direction")) {
        drawPreferredForward();
      }

      // *** Preferred Up Facing
      //EditorGUILayout.LabelField("Define Preferred Up", Style.Bold());
      EditorGUILayout.PropertyField(hasPreferredUp);
      if (hasPreferredUp.intValue > 0) {
        drawPreferredUp();
      }

      // *** Misc.
      EditorGUILayout.PropertyField(upchain);
      EditorGUILayout.PropertyField(downchain);
      EditorGUILayout.PropertyField(downstreamDistance);
      EditorGUILayout.PropertyField(upstreamDistance);

      serializedObject.ApplyModifiedProperties();

      //EditorGUILayout.LabelField("BASE");
      //base.OnInspectorGUI();
    }

    private void defineJointWeight()
    {
      EditorGUILayout.LabelField("Define Joint Weight", Style.Bold());

      EditorGUILayout.BeginHorizontal();
      EditorGUILayout.LabelField("", GUILayout.MaxWidth(30));
      EditorGUILayout.LabelField("Joint Weight:  ", GUILayout.MaxWidth(100));
      jointWeightValue = EditorGUILayout.Slider(jointWeightValue, 0, 1);
      EditorGUILayout.EndHorizontal();

      jointWeight.floatValue = jointWeightValue;
    }

    private void drawConstrainRotation()
    {
      showCone = scriptObject.GetComponent<ConeGizmo>() != null && constrainRotation.boolValue;

      EditorGUILayout.BeginHorizontal();
      EditorGUILayout.LabelField("", GUILayout.MaxWidth(30));
      EditorGUILayout.LabelField("X-Axis Limits", Style.Bold());
      EditorGUILayout.EndHorizontal();

      EditorGUILayout.BeginHorizontal();
      EditorGUILayout.LabelField("", GUILayout.MaxWidth(30));
      EditorGUILayout.LabelField("Left:", Style.RightAlign(), GUILayout.MaxWidth(60), GUILayout.ExpandWidth(true));
      EditorGUILayout.LabelField(leftLimit.ToString("#0"), Style.RightAlign(), GUILayout.MaxWidth(30));
      EditorGUILayout.MinMaxSlider(ref leftLimit, ref rightLimit, -90, 90);
      EditorGUILayout.LabelField(rightLimit.ToString("#0"), Style.LeftAlign(), GUILayout.MaxWidth(30));
      EditorGUILayout.LabelField(":Right", Style.LeftAlign(), GUILayout.MaxWidth(60), GUILayout.ExpandWidth(true));
      EditorGUILayout.EndHorizontal();

      EditorGUILayout.BeginHorizontal();
      EditorGUILayout.LabelField("", GUILayout.MaxWidth(30));
      EditorGUILayout.LabelField("Y-Axis Limits", Style.Bold());
      EditorGUILayout.EndHorizontal();

      EditorGUILayout.BeginHorizontal();
      EditorGUILayout.LabelField("", GUILayout.MaxWidth(30));
      EditorGUILayout.LabelField("Down:", Style.RightAlign(), GUILayout.MaxWidth(60), GUILayout.ExpandWidth(true));
      EditorGUILayout.LabelField(downLimit.ToString("#0"), Style.RightAlign(), GUILayout.MaxWidth(30));
      EditorGUILayout.MinMaxSlider(ref downLimit, ref upLimit, -90, 90);
      EditorGUILayout.LabelField(upLimit.ToString("#0"), Style.LeftAlign(), GUILayout.MaxWidth(30));
      EditorGUILayout.LabelField(":Up", Style.LeftAlign(), GUILayout.MaxWidth(60), GUILayout.ExpandWidth(true));
      EditorGUILayout.EndHorizontal();

      if (rightLimit < 0) { rightLimit = 0; }
      if (leftLimit > 0) { leftLimit = 0; }
      if (upLimit < 0) { upLimit = 0; }
      if (downLimit > 0) { downLimit = 0; }

      roteRight.floatValue = rightLimit;
      roteLeft.floatValue = leftLimit;
      roteUp.floatValue = upLimit;
      roteDown.floatValue = downLimit;

      EditorGUILayout.BeginHorizontal();
      EditorGUILayout.LabelField("", GUILayout.MaxWidth(30));
      showCone = GUILayout.Toggle(showCone, "  Show Cone");
      EditorGUILayout.EndHorizontal();

      if (showCone) {
        if (_coneGizmo == null) {
          _coneGizmo = scriptObject.GetComponent<ConeGizmo>();
        }
        if (_coneGizmo == null) {
          _coneGizmo = scriptObject.AddComponent<ConeGizmo>();
        }
        _coneGizmo.PosX = rightLimit;
        _coneGizmo.PosY = upLimit;
        _coneGizmo.NegX = leftLimit;
        _coneGizmo.NegY = downLimit;
      } else {
        if (_coneGizmo != null) {
          DestroyImmediate(_coneGizmo);
        }
      }
    }

    private void drawPreferredForward()
    {
      showPreferredDirection = scriptObject.GetComponent<LineGizmo>() != null && hasPreferredDirection.boolValue;

      EditorGUILayout.BeginHorizontal();
      EditorGUILayout.LabelField("", GUILayout.MaxWidth(30));
      EditorGUILayout.PropertyField(preferredDirection);
      EditorGUILayout.EndHorizontal();

      EditorGUILayout.BeginHorizontal();
      EditorGUILayout.LabelField("", GUILayout.MaxWidth(30));
      EditorGUILayout.LabelField("Preferred Facing Weight:  ", GUILayout.MaxWidth(160));
      preferredDirectionStrengthValue = EditorGUILayout.Slider(preferredDirectionStrengthValue, 0, 0.9f);
      EditorGUILayout.EndHorizontal();

      preferredDirectionStrength.floatValue = preferredDirectionStrengthValue;

      EditorGUILayout.BeginHorizontal();
      EditorGUILayout.LabelField("", GUILayout.MaxWidth(30));
      showPreferredDirection = GUILayout.Toggle(showPreferredDirection, "  Show Line");
      EditorGUILayout.EndHorizontal();

      if (showPreferredDirection) {
        if (_lineGizmo == null) {
          _lineGizmo = scriptObject.GetComponent<LineGizmo>();
        }
        if (_lineGizmo == null) {
          _lineGizmo = scriptObject.AddComponent<LineGizmo>();
        }
        _lineGizmo.RelativeForward = preferredDirection.vector3Value;
      } else {
        if (_lineGizmo != null) {
          DestroyImmediate(_lineGizmo);
        }
      }
    }

    private void drawPreferredUp()
    {
      var preferredUp = (FabrikJoint.PreferredUp)hasPreferredUp.intValue;

      showDesiredUp = scriptObject.GetComponent<MatchingArrowGizmo>() != null
        && preferredUp != FabrikJoint.PreferredUp.None;

      switch (preferredUp) {
        case FabrikJoint.PreferredUp.None:
          break;
        case FabrikJoint.PreferredUp.Interpolate:
          // this is interpolate
          EditorGUILayout.BeginHorizontal();
          EditorGUILayout.LabelField("", GUILayout.MaxWidth(30));
          EditorGUILayout.LabelField("Preference towards upchain:  ", GUILayout.MaxWidth(200));
          preferenceTowardsUpchainValue = EditorGUILayout.Slider(preferenceTowardsUpchainValue, 0, 1);
          EditorGUILayout.EndHorizontal();
          preferenceTowardsUpchain.floatValue = preferenceTowardsUpchainValue;
          showGizmoOption();
          break;
        case FabrikJoint.PreferredUp.Override:
          // this value is override
          EditorGUILayout.BeginHorizontal();
          EditorGUILayout.LabelField("", GUILayout.MaxWidth(30));
          EditorGUILayout.PropertyField(lookAtUpOverride);
          EditorGUILayout.EndHorizontal();
          showGizmoOption();
          break;
      }

      void showGizmoOption()
      {
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("", GUILayout.MaxWidth(30));
        showDesiredUp = GUILayout.Toggle(showDesiredUp, "  Show Desired Up");
        EditorGUILayout.EndHorizontal();
      }

      if (showDesiredUp) {
        if (_ArrowGizmo == null) {
          _ArrowGizmo = scriptObject.GetComponent<MatchingArrowGizmo>();
        }
        if (_ArrowGizmo == null) {
          _ArrowGizmo = scriptObject.AddComponent<MatchingArrowGizmo>();
        }

        var transformUp = upchain.objectReferenceValue as Transform;
        var transformDown = downchain.objectReferenceValue as Transform;

        switch (preferredUp) {
          case FabrikJoint.PreferredUp.None:
            break;
          case FabrikJoint.PreferredUp.Interpolate:
            var up = Vector3.Lerp(transformUp.up, transformDown.up, preferenceTowardsUpchainValue);
            _ArrowGizmo.RelativeUp = up;
            break;
          case FabrikJoint.PreferredUp.Override:
            _ArrowGizmo.RelativeUp = lookAtUpOverride.vector3Value;
            break;
          default:
            break;
        }

      } else {
        if (_ArrowGizmo != null) {
          DestroyImmediate(_ArrowGizmo);
        }
      }
    }
  }
}
