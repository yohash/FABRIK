using System.Collections.Generic;
using UnityEngine;

public class Fabrik : MonoBehaviour
{
  [Header("Assign in Inspector")]
  [SerializeField] private Transform parentTR;

  [SerializeField] private List<FabrikJoint> chain;

  [SerializeField] private List<Vector3> positions;

  [Header("Assign constants")]
  [SerializeField] private float locationTolerance = 0.05f;
  [SerializeField] private int maxIterations = 10;

  [Header("Target")]
  [SerializeField] private Transform target;

  void Start()
  {
    // store chain joint distances in a downstream fashion
    for (int i = 0; i < chain.Count - 1; i++) {
      chain[i].SetupDownstream(chain[i + 1]);
    }

    // setup fabrik chain by passing each joint its upstream joint
    chain[0].SetupUpstream(parentTR);
    for (int i = 1; i < chain.Count; i++) {
      chain[i].SetupUpstream(chain[i - 1].transform);
    }
  }

  void Update()
  {
    solve();
  }

  // ****************************************************************
  //		SOLVING THE IK
  // ****************************************************************
  private void solve()
  {
    // get the current positions of all components into newLocals
    positions.Clear();
    for (int i = 0; i < chain.Count; i++) {
      positions.Add(chain[i].transform.position);
    }

    int iter = 0;
    // loop over FABRIK algorithm
    float diffSq = (chain[chain.Count - 1].transform.position - target.position).sqrMagnitude;
    while (diffSq > (locationTolerance * locationTolerance)) {
      // perform the FABRIK algorithm. A backward pass, followed by a forward pass,
      // finally closed by movin the chain and computing tolerances
      backward();
      forward();
      move();

      // re-capture positions
      diffSq = (chain[chain.Count - 1].transform.position - target.position).sqrMagnitude;

      // break if over the iteration limit
      if (iter > maxIterations) { break; }
      iter += 1;
    }
  }

  private void backward()
  {
    // compute each new position in the backward-step
    // initialize by setting the last joint to the target position
    var v = target.position;
    positions[positions.Count - 1] = v;
    // cascade in the backward direction, upgrading each joint in 'newLocals' along the way
    for (int i = positions.Count - 1; i > 0; i--) {
      // get the new point by moving BACKWARD from current point, i, towards i-1 point
      var displace = positions[i - 1] - positions[i];
      v = positions[i] + displace.normalized * chain[i].StartOffsetDistance;
      // save that new position in this forwward step
      positions[i - 1] = v;
    }
  }

  private void forward()
  {
    // compute each new position in the forward-step
    // initialize by setting the first joint back to its origin
    var v = parentTR.position;
    positions[0] = v;
    // cascade in the forward direction, upgrading each joint in 'newLocals' along the way
    for (int i = 0; i < positions.Count - 1; i++) {
      // get the new point by moving FORWARD from current point, i, towards i+1 point
      var displace = positions[i + 1] - positions[i];
      v = positions[i] + displace.normalized * chain[i + 1].StartOffsetDistance;
      // verify the new point is a valid rotation
      v = chain[i].ConstrainPoint(v, positions[i]);
      // save that new position in this forwward step
      positions[i + 1] = v;
    }
  }

  private void move()
  {
    // set every other joint relative to the one prior
    for (int i = 0; i < positions.Count - 1; i++) {
      chain[i].transform.position = positions[i];
      chain[i].transform.LookAt(positions[i + 1]);
    }
    chain[chain.Count - 1].transform.LookAt(target.transform);
  }
}
