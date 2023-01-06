using System.Collections.Generic;
using UnityEngine;

public class FABRIK : MonoBehaviour
{
  [Header("Assign in Inspector")]
  [SerializeField] private Transform parentTR;

  [SerializeField] private List<FABRIKJoint> chain;
  [SerializeField] private List<Vector3> newGlobalPos;

  [SerializeField] private float locationTolerance = 0.05f;
  [SerializeField] private int maxIterations = 10;

  [Header("Target")]
  [SerializeField] private Transform target;

  // debugging var: remove
  [SerializeField] private float distanceFromTarget;


  void Start()
  {
    // setup chain
    for (int i = 0; i < chain.Count - 1; i++) {
      chain[i].LinkLength = chain[i + 1].StartOffsetDistance;
    }
    chain[0].setupFABRIKChain(parentTR);
    for (int i = 1; i < chain.Count; i++) {
      chain[i].setupFABRIKChain(chain[i - 1].transform);
    }

    //float d;
    //for (int i = 0; i < FABRIKChain.Count; i++) {
    //	d = FABRIKChain [i].startOffsetDistance;
    //}
  }

  void Update()
  {
    distanceFromTarget = (parentTR.position - target.position).magnitude;
    solve();
  }

  // ****************************************************************
  //		SOLVING THE IK
  // ****************************************************************
  public float DIFFSQ;
  private void solve()
  {
    Vector3 localTargetDir = target.position - parentTR.position;
    float dSQ = localTargetDir.sqrMagnitude;

    // get the current positions of all components into newLocals
    newGlobalPos.Clear();
    for (int i = 0; i < chain.Count; i++) {
      newGlobalPos.Add(chain[i].transform.position);
    }

    int iter = 0;
    // loop over FABRIK algorithm
    float diffSq = (chain[chain.Count - 1].transform.position - target.position).sqrMagnitude;
    DIFFSQ = diffSq;
    while (diffSq > (locationTolerance * locationTolerance)) {
      // perform backward pass
      backward();
      // perform forward pass
      forward();
      // move
      moveChain();
      // re-capture positions
      diffSq = (chain[chain.Count - 1].transform.position - target.position).sqrMagnitude;
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
    Vector3 v;
    Vector3 displace;
    // initialize by setting the first joint back to its origin
    v = parentTR.position;
    newGlobalPos[0] = v;
    // cascade in the forward direction, upgrading each joint in 'newLocals' along the way
    for (int i = 0; i < newGlobalPos.Count - 1; i++) {
      // get the new point by moving FORWARD from current point, i, towards i+1 point
      displace = newGlobalPos[i + 1] - newGlobalPos[i];
      v = newGlobalPos[i] + displace.normalized * chain[i + 1].StartOffsetDistance;
      // verify the new point is a valid rotation
      v = chain[i].constrainPoint(v, newGlobalPos[i]);
      // save that new position in this forwward step
      newGlobalPos[i + 1] = v;
    }
  }

  private void backward()
  {
    // compute each new position in the backward-step
    Vector3 v;
    Vector3 displace;
    // initialize by setting the last joint to the target position
    v = target.position;
    newGlobalPos[newGlobalPos.Count - 1] = v;
    // cascade in the backward direction, upgrading each joint in 'newLocals' along the way
    for (int i = newGlobalPos.Count - 1; i > 0; i--) {
      // get the new point by moving BACKWARD from current point, i, towards i-1 point
      displace = newGlobalPos[i - 1] - newGlobalPos[i];
      v = newGlobalPos[i] + displace.normalized * chain[i].StartOffsetDistance;
      // save that new position in this forwward step
      newGlobalPos[i - 1] = v;
    }
  }


  private void moveChain()
  {
    // set every other joint relative to the one prior
    for (int i = 0; i < newGlobalPos.Count - 1; i++) {
      chain[i].transform.position = newGlobalPos[i];
      chain[i].transform.LookAt(newGlobalPos[i + 1]);
    }
    chain[chain.Count - 1].transform.LookAt(target.transform);
  }
}
