using UnityEngine;

/// <summary>
/// Transcribed from www.alanzucconi.com/2017/04/10/robotic-arms
/// </summary>
public class CustomIK : MonoBehaviour
{
  public Transform parentTR;

  public RobotJoint[] Joints;

  public float samplingDistance = 0.2f;
  public float samplingScale = 0.25f;
  public float samplingMin = 0.01f;
  public float samplingMax = 10f;

  public float learningRate = 10f;
  public float learningScale = 500f;
  public float learningMin = 10f;
  public float learningMax = 2000f;

  public float distanceThreshold = 0.05f;

  public GameObject IKtarget;

  public float DEBUG_DISTANCE = 0f;
  // *****************************************************************************
  //		MONOBEHAVIOURS
  // *****************************************************************************
  void Update()
  {
    if (IKtarget != null) {
      float[] theAngles = getAngles();
      InverseKinematics(IKtarget.transform.position, theAngles);
    }
  }
  // *****************************************************************************
  //		PUBLIC ACCESSORS
  // *****************************************************************************
  public void setIKTarget(GameObject _target)
  {
    IKtarget = _target;
  }
  // *****************************************************************************
  //		INVERSE KINEMATICS
  // *****************************************************************************
  public float[] getAngles()
  {
    float[] angles = new float[Joints.Length];
    for (int i = 0; i < Joints.Length; i++) {
      angles[i] = Joints[i].getLocalRotation();
    }
    return angles;
  }

  public void InverseKinematics(Vector3 target, float[] angles)
  {
    float d = distanceFromTarget(target, angles);
    // set an early exit criteria
    if (d < distanceThreshold) {
      return;
    }
    // adaptive learning rate
    learningRate = Mathf.Clamp(d * learningScale, learningMin, learningMax);

    // perform inverse kinematics
    for (int i = Joints.Length - 1; i >= 0; i--) {
      // Gradient descent
      float grad = PartialGradient(target, angles, i);
      angles[i] -= learningRate * grad;

      //			// clamp to min-max angles
      //			angles[i] = Mathf.Clamp(angles[i], Joints[i].MinAngle, Joints[i].MaxAngle);

      // assign the new angle
      Joints[i].setLocal_axialRotation(angles[i]);

      // check early exit criteria
      d = distanceFromTarget(target, angles);
      if (d < distanceThreshold) {
        return;
      }
    }
  }

  public float PartialGradient(Vector3 target, float[] angles, int i)
  {
    // save the angle, it will be restored later
    float angle = angles[i];
    // Gradient : [FF(x+SamplingDistance)
    float fx = distanceFromTarget(target, angles);

    // adaptive sampling distance
    samplingDistance = Mathf.Clamp(fx * samplingScale, samplingMin, samplingMax);

    // check the mext point
    angles[i] += samplingDistance;
    float fx_plus_d = distanceFromTarget(target, angles);

    float gradient = (fx_plus_d - fx) / samplingDistance;

    // restore the value
    angles[i] = angle;

    return gradient;
  }

  public float distanceFromTarget(Vector3 target, float[] angles)
  {
    Vector3 point = ForwardKinematics(angles);

    float dd = Vector3.Distance(point, target);
    DEBUG_DISTANCE = dd;

    return dd;
  }


  public Vector3 ForwardKinematics(float[] angles)
  {
    Vector3 prevPoint = Joints[0].getPosition();
    Quaternion rote = Quaternion.identity;
    // if this robot-arm is nested beneath a parent that rotates,
    // if will need to consider this parent's rotation
    if (parentTR != null)
      rote *= parentTR.rotation;

    for (int i = 1; i < Joints.Length; i++) {
      var angle = angles[i - 1];
      var axis = Joints[i - 1].axis;
      var offset = Joints[i].startOffset;

      rote *= Quaternion.AngleAxis(angle, axis);
      Vector3 nextPoint = prevPoint + rote * offset;

      prevPoint = nextPoint;
    }

    return prevPoint;
  }
}
