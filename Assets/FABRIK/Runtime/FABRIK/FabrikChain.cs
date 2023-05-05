using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Yohash.FABRIK
{
  public class FabrikChain : MonoBehaviour
  {
    public Transform SecondLink {
      get {
        // if the chain is shorter than 2 elements ([0] and [1]) it is not a chain
        return chain[1].Transform;
      }
    }

    [Header("Public References")]
    // TODO - this is freely assigned by external elements; write a better localized storage
    public Vector3 LocalRelativeForward;

    // TODO - be nice to keep this var private; re-write SpiderFabrik to remove this dependence
    public List<Vector3> Positions;

    private List<IJoint> chain;

    [Header("Assign Tolerances")]
    [SerializeField] private float locationTolerance = 0.005f;
    [SerializeField] private int maxIterations = 10;
    public int MaxIterations {
      get { return MaxIterations; }
      set { maxIterations = value; }
    }

    [Header("Tracking Target")]
    [SerializeField] private Transform _target;
    public Transform Target {
      get { return _target; }
      set { _target = value; }
    }

    public Pose TargetPose {
      get { return _target.ToPose(); }
    }

    public float ChainLength {
      get {
        return chain.Sum(link => link.DownstreamDistance);
      }
    }

    // ****************************************************************
    //		DEBUGGING TOOLS - build these into custom editor
    // ****************************************************************
    public bool DEBUG_SHOW_SOLUTION;

    void Update()
    {
      if (DEBUG_SHOW_SOLUTION) {
        drawSolution();
      }
    }

    private void drawSolution()
    {
      Debug.DrawRay(Positions[0], transform.position - Positions[0], Color.green);
      for (int i = 1; i < Positions.Count; i++) {
        Debug.DrawRay(Positions[i], Positions[i - 1] - Positions[i], Color.green);
      }
    }

    // ****************************************************************
    //		MONOBEHAVIOURS
    // ****************************************************************
    private void Awake()
    {
      chain = new List<IJoint>();
      chain.AddRange(GetComponentsInChildren<IJoint>());

      // send the variables for constraint checking in an upstream direction
      chain[0].SetupUpstream(transform);
      for (int i = 1; i < chain.Count; i++) {
        chain[i].SetupUpstream(chain[i - 1].Transform);
      }

      // setup chain in a downstream direction
      for (int i = 0; i < chain.Count - 1; i++) {
        chain[i].SetupDownstream(chain[i + 1]);
      }
    }

    /// <summary>
    /// Intializes the FABRIK chain by setting the local relative forward
    /// TODO - consider re-working this into a non-monobehaviour class that
    ///         has the "Start()" and "Initialize" methods baked in to class
    ///         instantiation
    /// </summary>
    public void Intialize(Pose parent)
    {
      var joint = SecondLink;

      float rt = Vector3.Dot(parent.forward, joint.right);
      float fwd = Vector3.Dot(parent.forward, joint.forward);
      float up = Vector3.Dot(parent.forward, joint.up);

      var v3 = new Vector3(rt, up, fwd);

      // set the two important variables on the FABRIK chain
      LocalRelativeForward = v3;
    }

    // ****************************************************************
    //		PUBLIC ACCESSORS TO SOLVE THE IK
    // ****************************************************************
    public bool IsWithinTolerance {
      get {
        return (chain[chain.Count - 1].Transform.position - _target.position).sqrMagnitude
          <= locationTolerance * locationTolerance
          && chain[chain.Count - 1].Transform.rotation == _target.rotation;
      }
    }

    public void Solve()
    {
      int iter = 0;
      // loop over FABRIK algorithm
      while (!IsWithinTolerance) {
        // perform the FABRIK algorithm. A backward pass, followed by a forward pass,
        // finally closed by movin the chain and computing tolerances
        Backward();
        Forward();
        Move();

        // break if over the iteration limit
        if (iter > maxIterations) { break; }
        iter += 1;
      }
    }

    public void Backward()
    {
      // The backward process always initiates the FABRIK algorithm.
      // Initialize our positions array here
      initSolver();

      // compute each new position in the backward-step
      // initialize by setting the last joint to the target position
      Positions[Positions.Count - 1] = _target.position;
      // cascade in the backward direction, upgrading each joint along the way
      for (int i = Positions.Count - 1; i > 0; i--) {
        // get the new point by moving BACKWARD from current point, i, towards i-1 point
        var displace = Positions[i - 1] - Positions[i];
        var final = Positions[i] + displace.normalized * chain[i].UpstreamDistance;
        // save that new position in this forwward step
        Positions[i - 1] = final;
      }
    }

    public void Forward()
    {
      // compute each new position in the forward-step
      // initialize by setting the first joint back to its origin
      Positions[0] = transform.position;
      // cascade in the forward direction, upgrading each joint along the way
      for (int i = 0; i < Positions.Count - 1; i++) {
        // get the new point by moving FORWARD from current point, i, towards i+1 point
        // tell the chain at [i] to constrain this [i+1] (downstream) point by applying
        // [i]th joint's-specific constraints, to include link length
        var constrained = chain[i].ConstrainDownstreamPoint(Positions[i + 1]);

        // v is the new global point, so we can now interpolate between
        //   <currentPosition> = Positions[i], and 'constrained', by weight,
        //   to add 'sluggishness' to the joint
        var weighted = Vector3.Lerp(Positions[i], constrained, chain[i].JointWeight);

        // get a new displacement vector to the constrained point
        // then, normalize and scale this vector, adding to our current location
        var finalDirection = weighted - Positions[i];
        var final = Positions[i] + finalDirection.normalized * chain[i].DownstreamDistance;

        // finally save that new position in this forwward step
        Positions[i + 1] = final;
      }
    }

    public void Move()
    {
      // set every other joint relative to the one prior
      for (int i = 0; i < Positions.Count - 1; i++) {
        chain[i].AssignPosition(Positions[i]);
        chain[i].LookAtPosition(Positions[i + 1]);
        chain[i].LookAtUp(Vector3.up);
      }
      chain[chain.Count - 1].AssignPosition(Positions[Positions.Count - 1]);
      chain[chain.Count - 1].LookAtPosition(_target.position + _target.forward);
      chain[chain.Count - 1].LookAtUp(_target.up);
    }

    private void initSolver()
    {
      // initiates the FABRIK process,
      // make a copy-array of our current positions to manipulate
      // get the current positions of all components
      Positions.Clear();
      for (int i = 0; i < chain.Count; i++) {
        Positions.Add(chain[i].Transform.position);
      }
    }
  }
}
