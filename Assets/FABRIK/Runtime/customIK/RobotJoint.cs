using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RobotJoint : MonoBehaviour
{

  Transform tr;

  public Vector3 axis;
  public Vector3 startOffset;

  public float MinAngle = -360f;
  public float MaxAngle = 360f;

  public float DEBUG_LocalRote;

  // *****************************************************************************
  //		MONOBEHAVIOURS
  // *****************************************************************************
  void Awake()
  {
    tr = transform;
    startOffset = tr.localPosition;

    axis.Normalize();
  }

  // *****************************************************************************
  //		PUBLIC ACCESSORS
  // *****************************************************************************
  public Vector3 getPosition()
  {
    return tr.position;
  }

  public Quaternion getRotation()
  {
    return tr.rotation;
  }

  public float getLocalRotation()
  {
    float rote = Vector3.Dot(tr.localRotation.eulerAngles, axis);
    if ((rote > 180f) && (axis != (new Vector3(1, 0, 0)))) {
      rote -= 360f;
    }
    if ((tr.localRotation.eulerAngles.y > 170) && (tr.localRotation.eulerAngles.z > 170) && (axis == (new Vector3(1, 0, 0)))) {
      rote -= 180f;
      rote *= -1;
    }
    DEBUG_LocalRote = rote;

    return rote;
  }

  public void setLocal_axialRotation(float roteAngle)
  {
    // if RoteAngle is outside our Min-Max (ie. sent in 275-deg, but we accept -180 to 0)
    // we'll try the (+) and (-) alternative solutions for it
    float newAngle = roteAngle;
    //		if (!angleIsValid (newAngle)) {
    //			newAngle = roteAngle - 360f;
    //			if (!angleIsValid (newAngle)) {
    //				newAngle = roteAngle + 360f;
    //			}
    //		}
    //		if (angleIsValid (newAngle))
    setAxialRotation(newAngle);
  }

  // *****************************************************************************
  //		PRIVATE HELPeR FUNCTIONS
  // *****************************************************************************
  private void setAxialRotation(float rotationAngle)
  {
    float actualAngle = rotationAngle - getLocalRotation();

    Quaternion roteQuat = Quaternion.AngleAxis(actualAngle, axis);
    tr.rotation *= roteQuat;
    DEBUG_LocalRote = getLocalRotation();
  }

  private bool angleIsValid(float rotation)
  {
    return ((rotation > MinAngle) && (rotation < MaxAngle));
  }
}
