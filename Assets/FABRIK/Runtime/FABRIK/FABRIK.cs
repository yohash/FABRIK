using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FABRIK : MonoBehaviour
{
  public Transform parentTR;

  public List<FABRIKJoint> FABRIKChain;
  public List<Vector3> newGlobalPos;

  public float locationTolerance = 0.05f;
  public float locationTolSq;
  public int maxIterations = 10;

  public bool ____target____;
  public Transform targetTR;

  public float currentD_fromTar;


  void Start()
  {
    // setup chain
    for (int i = 0; i < FABRIKChain.Count - 1; i++) {
      FABRIKChain[i].linkLength = FABRIKChain[i + 1].startOffsetDistance;
    }
    FABRIKChain[0].setupFABRIKChain(parentTR);
    for (int i = 1; i < FABRIKChain.Count; i++) {
      FABRIKChain[i].setupFABRIKChain(FABRIKChain[i - 1].myTR);
    }

    //float d;
    //for (int i = 0; i < FABRIKChain.Count; i++) {
    //	d = FABRIKChain [i].startOffsetDistance;
    //}

    locationTolSq = locationTolerance * locationTolerance;
  }

  void Update()
  {
    currentD_fromTar = (parentTR.position - targetTR.position).sqrMagnitude;
    solve();
  }

  // ****************************************************************
  //		SOLVING THE IK
  // ****************************************************************
  public float DIFFSQ;
  private void solve()
  {
    Vector3 localTargetDir = targetTR.position - parentTR.position;
    float dSQ = localTargetDir.sqrMagnitude;

    // get the current positions of all components into newLocals
    newGlobalPos.Clear();
    for (int i = 0; i < FABRIKChain.Count; i++) {
      newGlobalPos.Add(FABRIKChain[i].myTR.position);
    }

    int iter = 0;
    // loop over FABRIK algorithm
    float diffSq = (FABRIKChain[FABRIKChain.Count - 1].myTR.position - targetTR.position).sqrMagnitude;
    DIFFSQ = diffSq;
    while (diffSq > (locationTolerance * locationTolerance)) {
      // perform backward pass
      backward();
      // perform forward pass
      forward();
      // move
      moveChain();
      // re-capture positions
      diffSq = (FABRIKChain[FABRIKChain.Count - 1].myTR.position - targetTR.position).sqrMagnitude;
      // break if over the limit
      iter += 1;
      if (iter > maxIterations) {
        break;
      }
    }
  }

  private void forward()
  {
    // compute each new position in the forward-step
    Vector3 v, displace;
    // initialize by setting the first joint back to its origin
    v = parentTR.position;
    newGlobalPos[0] = v;
    // cascade in the forward direction, upgrading each joint in 'newLocals' along the way
    for (int i = 0; i < newGlobalPos.Count - 1; i++) {
      // get the new point by moving FORWARD from current point, i, towards i+1 point
      displace = newGlobalPos[i + 1] - newGlobalPos[i];
      v = newGlobalPos[i] + displace.normalized * FABRIKChain[i + 1].startOffsetDistance;
      // verify the new point is a valid rotation
      v = FABRIKChain[i].constrainPoint(v, newGlobalPos[i]);
      // save that new position in this forwward step
      newGlobalPos[i + 1] = v;
    }
  }

  private void backward()
  {
    // compute each new position in the backward-step
    Vector3 v, displace;
    // initialize by setting the last joint to the target position
    v = targetTR.position;
    newGlobalPos[newGlobalPos.Count - 1] = v;
    // cascade in the backward direction, upgrading each joint in 'newLocals' along the way
    for (int i = newGlobalPos.Count - 1; i > 0; i--) {
      // get the new point by moving BACKWARD from current point, i, towards i-1 point
      displace = newGlobalPos[i - 1] - newGlobalPos[i];
      v = newGlobalPos[i] + displace.normalized * FABRIKChain[i].startOffsetDistance;
      // save that new position in this forwward step
      newGlobalPos[i - 1] = v;
    }
  }


  private void moveChain()
  {
    // set every other joint relative to the one prior
    for (int i = 0; i < newGlobalPos.Count - 1; i++) {
      FABRIKChain[i].myTR.position = newGlobalPos[i];
      FABRIKChain[i].myTR.LookAt(newGlobalPos[i + 1]);
    }
    FABRIKChain[FABRIKChain.Count - 1].myTR.LookAt(targetTR.transform);
  }
}
