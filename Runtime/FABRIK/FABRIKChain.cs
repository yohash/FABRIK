using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FABRIKChain : MonoBehaviour
{
  [Header("Public References")]
  public Transform chainBase;
  public Transform chainHead;
  public Transform chainSecond;

  public Vector3 myLocalRelativeForward;

  public List<Vector3> newGlobalPos;

  [Header("Assign the Chain")]
  public List<FABRIKJoint> theChain;

  [Header("Assign Tolerances")]
  public float locationTolerance = 0.005f;
  private float locationTolSq;
  public int maxIterations = 10;

  [Header("Tracking Target")]
  public Transform targetTR;

  Transform chainEnd;
  Transform tr;

  // ****************************************************************
  //		MONOBEHAVIOURS
  // ****************************************************************
  void Awake()
  {
    tr = transform;
    // if the chain is shorter than 2 elements ([0] and [1]) it is not a chain
    chainHead = theChain[0].transform;
    chainSecond = theChain[1].transform;
  }

  void Start()
  {
    // setup chain in a downstream direction
    for (int i = 0; i < theChain.Count - 1; i++) {
      theChain[i].linkLength = theChain[i + 1].startOffsetDistance;
      // set up the chain's preferred direction vectors
      if (theChain[i].hasPreferredDirection) {
        theChain[i].preferredActualForward = theChain[i].preferredRelativeForward * theChain[i].linkLength;
      }
    }
    // send the variables for constraint checking in an upstream direction
    theChain[0].setupFABRIKChain(tr);
    for (int i = 1; i < theChain.Count; i++) {
      theChain[i].setupFABRIKChain(theChain[i - 1].myTR);
    }

    locationTolSq = locationTolerance * locationTolerance;

    chainEnd = theChain[theChain.Count - 1].transform;
  }

  // ****************************************************************
  //		PUBLIC ACCESSORS TO SOLVE THE IK
  // ****************************************************************
  public float getDistanceSq_fromTarget()
  {
    return (chainEnd.position - targetTR.position).sqrMagnitude;
  }

  public bool targetDistance_isWithinTolerance()
  {
    if (getDistanceSq_fromTarget() <= locationTolSq) {
      return true;
    }
    return false;
  }

  public void backward()
  {
    // the BACKWARD process always initiates the FABRIK process,
    // so we will make a copy-array of our current positions to manipulate
    // get the current positions of all components into newLocals
    newGlobalPos.Clear();
    for (int i = 0; i < theChain.Count; i++) {
      newGlobalPos.Add(theChain[i].myTR.position);
    }

    // compute each new position in the backward-step
    Vector3 v, displace;
    // initialize by setting the last joint to the target position
    v = targetTR.position;
    newGlobalPos[newGlobalPos.Count - 1] = v;
    // cascade in the backward direction, upgrading each joint in 'newLocals' along the way
    for (int i = newGlobalPos.Count - 1; i > 0; i--) {
      // get the new point by moving BACKWARD from current point, i, towards i-1 point
      displace = newGlobalPos[i - 1] - newGlobalPos[i];
      v = newGlobalPos[i] + displace.normalized * theChain[i].startOffsetDistance;
      // save that new position in this forwward step
      newGlobalPos[i - 1] = v;
    }
  }

  public void forward(Vector3 basePos)
  {
    // compute each new position in the forward-step
    Vector3 v, displace;
    // initialize by setting the first joint back to its origin
    v = basePos;
    newGlobalPos[0] = v;
    // cascade in the forward direction, upgrading each joint in 'newLocals' along the way
    for (int i = 0; i < newGlobalPos.Count - 1; i++) {
      // get the new point by moving FORWARD from current point, i, towards i+1 point
      displace = newGlobalPos[i + 1] - newGlobalPos[i];

      // vector 'displace' should give us enough info to determine conic constraints
      v = theChain[i].constrainPoint(newGlobalPos[i] + displace, newGlobalPos[i]);

      // v is the new global point, so we can now interpolate between
      //   <currentPosition> = newGlobalPos[i], and 'v', by weight, to add 'sluggishness' to the joint
      if (theChain[i].jointWeight < 1) {
        // joint weight is not one, apply the weight
        v = Vector3.Lerp(newGlobalPos[i], v, theChain[i].jointWeight);
      }

      // get a new displacement vector to the constrained point
      displace = v - newGlobalPos[i];
      // normalize and scale this vector, adding to our current location
      v = newGlobalPos[i] + displace.normalized * theChain[i + 1].startOffsetDistance;

      // save that new position in this forwward step
      newGlobalPos[i + 1] = v;
    }
  }

  public void moveChain()
  {
    // set every other joint relative to the one prior
    for (int i = 0; i < newGlobalPos.Count - 1; i++) {
      theChain[i].myTR.position = newGlobalPos[i];
      theChain[i].LookAt_NextJoint(newGlobalPos[i + 1]);
    }
    theChain[theChain.Count - 1].LookAt_NextJoint(targetTR.position);
  }
}
