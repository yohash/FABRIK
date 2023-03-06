using System.Collections.Generic;
using UnityEngine;

namespace Yohash.FABRIK
{
  public class Fabrik : MonoBehaviour
  {
    [Header("Assign in Inspector")]
    [SerializeField] private Transform parentTR;
    [SerializeField] private List<IJoint> chain;
    [SerializeField] private List<Vector3> positions;

    [Header("Assign constants")]
    [SerializeField] private float locationTolerance = 0.05f;
    [SerializeField] private int maxIterations = 10;

    [Header("Target")]
    [SerializeField] private Transform target;

    public bool DEBUG_SHOW_SOLUTION;

    void Start()
    {
      chain = new List<IJoint>();
      chain.AddRange(GetComponentsInChildren<IJoint>());

      // setup fabrik chain by passing each joint its upstream joint
      chain[0].SetupUpstream(parentTR);
      for (int i = 1; i < chain.Count; i++) {
        chain[i].SetupUpstream(chain[i - 1].Transform);
      }

      // store chain joint distances in a downstream fashion
      for (int i = 0; i < chain.Count - 1; i++) {
        chain[i].SetupDownstream(chain[i + 1]);
      }
    }

    void Update()
    {
      if (target == null) { return; }
      solve();

      if (DEBUG_SHOW_SOLUTION) {
        drawSolution();
      }
    }

    // ****************************************************************
    //		SOLVING THE IK
    // ****************************************************************
    private bool isWithinTolerance {
      get {
        return (chain[chain.Count - 1].Transform.position - target.position).sqrMagnitude
          <= locationTolerance * locationTolerance
          && chain[chain.Count - 1].Transform.rotation == target.rotation;
      }
    }

    private void solve()
    {
      // get the current positions of all components into newLocals
      initSolver();

      int iter = 0;
      // loop over FABRIK algorithm
      while (!isWithinTolerance) {
        // perform the FABRIK algorithm. A backward pass, followed by a forward pass,
        // finally closed by movin the chain and computing tolerances
        backward();
        forward();
        move();

        // break if over the iteration limit
        if (iter > maxIterations) { break; }
        iter += 1;
      }
    }

    private void backward()
    {
      // compute each new position in the backward-step
      // initialize by setting the last joint to the target position
      positions[positions.Count - 1] = target.position;
      // cascade in the backward direction, upgrading each joint in 'newLocals' along the way
      for (int i = positions.Count - 1; i > 0; i--) {
        // get the new point by moving BACKWARD from current point, i, towards i-1 point
        var displace = positions[i - 1] - positions[i];
        var v = positions[i] + displace.normalized * chain[i].StartOffsetDistance;
        // save that new position in this forwward step
        positions[i - 1] = v;
      }
    }

    private void forward()
    {
      // compute each new position in the forward-step
      // initialize by setting the first joint back to its origin
      positions[0] = parentTR.position;
      // cascade in the forward direction, upgrading each joint in 'newLocals' along the way
      for (int i = 0; i < positions.Count - 1; i++) {
        // get the new point by moving FORWARD from current point, i, towards i+1 point
        var displace = positions[i + 1] - positions[i];
        // vector 'displace' should give us enough info to determine conic constraints
        var constrained = chain[i].ConstrainPoint(positions[i] + displace, positions[i]);
        // v is the new global point, so we can now interpolate between
        //   <currentPosition> = newGlobalPos[i], and 'v', by weight, to add 'sluggishness' to the joint
        var weighted = Vector3.Lerp(positions[i], constrained, chain[i].JointWeight);

        // get a new displacement vector to the constrained point
        // then, normalize and scale this vector, adding to our current location
        var finalDirection = weighted - positions[i];
        var final = positions[i] + finalDirection.normalized * chain[i + 1].StartOffsetDistance;

        // finally save that new position in this forwward step
        positions[i + 1] = final;
      }
    }

    private void move()
    {
      // set every other joint relative to the one prior
      for (int i = 0; i < positions.Count - 1; i++) {
        chain[i].AssignPosition(positions[i]);
        chain[i].LookAtPosition(positions[i + 1]);
        chain[i].LookAtUp(Vector3.up);
      }
      chain[chain.Count - 1].AssignPosition(positions[positions.Count - 1]);
      chain[chain.Count - 1].LookAtPosition(target.position + target.forward);
      chain[chain.Count - 1].LookAtUp(target.up);
    }

    private void initSolver()
    {
      // initiates the FABRIK process,
      // make a copy-array of our current positions to manipulate
      // get the current positions of all components
      positions.Clear();
      for (int i = 0; i < chain.Count; i++) {
        positions.Add(chain[i].Transform.position);
      }
    }

    private void drawSolution()
    {
      Debug.DrawRay(positions[0], parentTR.position - positions[0], Color.green);
      for (int i = 1; i < positions.Count; i++) {
        Debug.DrawRay(positions[i], positions[i - 1] - positions[i], Color.green);
      }
    }
  }
}
