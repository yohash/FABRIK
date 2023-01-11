using System.Collections.Generic;
using UnityEngine;

public class FabrikChain : MonoBehaviour
{
  [Header("Public References")]
  public Transform ChainBase;
  public Transform ChainHead;
  public Transform ChainSecond;

  public Vector3 LocalRelativeForward;

  public List<Vector3> NewGlobalPos;

  [Header("Assign the Chain")]
  [SerializeField] private List<FabrikJoint> chain;

  [Header("Assign Tolerances")]
  [SerializeField] private float locationTolerance = 0.005f;
  private float locationTolSq;
  [SerializeField] private int maxIterations = 10;

  [Header("Tracking Target")]
  [SerializeField] private Transform target;
  [SerializeField] private Transform chainEnd;

  // ****************************************************************
  //		MONOBEHAVIOURS
  // ****************************************************************
  void Awake()
  {
    // if the chain is shorter than 2 elements ([0] and [1]) it is not a chain
    ChainHead = chain[0].transform;
    ChainSecond = chain[1].transform;
  }

  void Start()
  {
    // setup chain in a downstream direction
    for (int i = 0; i < chain.Count - 1; i++) {
      chain[i].LinkLength = chain[i + 1].StartOffsetDistance;
      // set up the chain's preferred direction vectors
      if (chain[i].HasPreferredDirection) {
        chain[i].PreferredActualForward = chain[i].PreferredRelativeForward * chain[i].LinkLength;
      }
    }
    // send the variables for constraint checking in an upstream direction
    chain[0].SetupFabrikChain(transform);
    for (int i = 1; i < chain.Count; i++) {
      chain[i].SetupFabrikChain(chain[i - 1].transform);
    }

    locationTolSq = locationTolerance * locationTolerance;

    chainEnd = chain[chain.Count - 1].transform;
  }


  // ****************************************************************
  //		PUBLIC ACCESSORS TO SOLVE THE IK
  // ****************************************************************
  public bool DistanceIsWithinTolerance()
  {
    var distanceFromTargetSq = (chainEnd.position - target.position).sqrMagnitude;
    if (distanceFromTargetSq <= locationTolSq) {
      return true;
    }
    return false;
  }

  public void backward()
  {
    // the BACKWARD process always initiates the FABRIK process,
    // so we will make a copy-array of our current positions to manipulate
    // get the current positions of all components into newLocals
    NewGlobalPos.Clear();
    for (int i = 0; i < chain.Count; i++) {
      NewGlobalPos.Add(chain[i].transform.position);
    }

    // compute each new position in the backward-step
    // initialize by setting the last joint to the target position
    var v = target.position;
    NewGlobalPos[NewGlobalPos.Count - 1] = v;
    // cascade in the backward direction, upgrading each joint in 'newLocals' along the way
    for (int i = NewGlobalPos.Count - 1; i > 0; i--) {
      // get the new point by moving BACKWARD from current point, i, towards i-1 point
      var displace = NewGlobalPos[i - 1] - NewGlobalPos[i];
      v = NewGlobalPos[i] + displace.normalized * chain[i].StartOffsetDistance;
      // save that new position in this forwward step
      NewGlobalPos[i - 1] = v;
    }
  }

  public void forward(Vector3 basePos)
  {
    // compute each new position in the forward-step
    // initialize by setting the first joint back to its origin
    var v = basePos;
    NewGlobalPos[0] = v;
    // cascade in the forward direction, upgrading each joint in 'newLocals' along the way
    for (int i = 0; i < NewGlobalPos.Count - 1; i++) {
      // get the new point by moving FORWARD from current point, i, towards i+1 point
      var displace = NewGlobalPos[i + 1] - NewGlobalPos[i];

      // vector 'displace' should give us enough info to determine conic constraints
      v = chain[i].ConstrainPoint(NewGlobalPos[i] + displace, NewGlobalPos[i]);

      // v is the new global point, so we can now interpolate between
      //   <currentPosition> = newGlobalPos[i], and 'v', by weight, to add 'sluggishness' to the joint
      if (chain[i].JointWeight < 1) {
        // joint weight is not one, apply the weight
        v = Vector3.Lerp(NewGlobalPos[i], v, chain[i].JointWeight);
      }

      // get a new displacement vector to the constrained point
      displace = v - NewGlobalPos[i];
      // normalize and scale this vector, adding to our current location
      v = NewGlobalPos[i] + displace.normalized * chain[i + 1].StartOffsetDistance;

      // save that new position in this forwward step
      NewGlobalPos[i + 1] = v;
    }
  }

  public void moveChain()
  {
    // set every other joint relative to the one prior
    for (int i = 0; i < NewGlobalPos.Count - 1; i++) {
      chain[i].transform.position = NewGlobalPos[i];
      chain[i].LookAt_NextJoint(NewGlobalPos[i + 1]);
    }
    chain[chain.Count - 1].LookAt_NextJoint(target.position);
  }
}
