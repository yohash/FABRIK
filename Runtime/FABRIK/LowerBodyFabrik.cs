using UnityEngine;
using Yohash.Bezier;

namespace Yohash.FABRIK
{
  public enum foot_placement
  {
    STANDING,
    LEFT_FOOT_STEPPING,
    RIGHT_FOOT_STEPPING,
  }

  public class LowerBodyFabrik : MonoBehaviour
  {
    public int maxIters = 10;

    [Header("Assign Torso Tracking Point")]
    public Transform UpperTorso;

    // The left and right leg FRABRIK Chains
    [Header("Assign FABRIK Chains")]
    [SerializeField] private FabrikChain leftLeg;
    [SerializeField] private FabrikChain rightLeg;

    [Header("Assign Foot Targets")]
    [SerializeField] private GameObject leftFoot;
    [SerializeField] private GameObject rightFoot;

    [Header("Assign Variable values")]
    [SerializeField] private float footHeight = 0.2f;
    [SerializeField] private float maxFootDelta = 0.25f;
    [SerializeField] private float stepMaxTime = 1f;

    [SerializeField] private Vector3 defaultLeftOffset = new Vector3(-0.25f, -1f, 0f);
    [SerializeField] private Vector3 defaultRightOffset = new Vector3(0.25f, -1f, 0f);

    [Header("Tracked private vars")]
    [SerializeField] private Vector3 leftFootWorldPosition;
    [SerializeField] private Vector3 rightFootWorldPosition;
    [SerializeField] private foot_placement currentSteps = foot_placement.STANDING;

    // private vars
    private Vector3 leftFootDestination;
    private Vector3 leftFootStartPoint;
    private Vector3 rightFootDestination;
    private Vector3 rightFootStartPoint;

    private Vector3 mechCurrentVelocity = Vector3.zero;
    private float timeStepStarted;
    private Vector3 lastPosition;

    // ****************************************************************
    //    MONOBEHAVIOURS
    // ****************************************************************
    void Start()
    {
      var pose = transform.ToPose();
      leftLeg.Intialize(pose);
      rightLeg.Intialize(pose);
    }

    private void Update()
    {
      var dt = Time.deltaTime;
      if (dt > 0) {
        SolveIK((transform.position - lastPosition) / Time.deltaTime);
      }
      lastPosition = transform.position;
    }

    // ****************************************************************
    //    PUBLIC ACCESSORS
    // ****************************************************************
    public void SolveIK(Vector3 velocity)
    {
      mechCurrentVelocity = velocity;
      updateFootsteps();
      solve();
    }

    // ****************************************************************
    //    solver
    // ****************************************************************
    private void solve()
    {
      // setup our iteration counter
      int iter = 0;

      // always update the waist node to pair with the torso
      transform.position = UpperTorso.position;

      // ensure both feet are postiioned at their world position
      leftFoot.transform.position = leftFootWorldPosition;
      rightFoot.transform.position = rightFootWorldPosition;

      // make sure both feet are aligned with the waist's forward
      leftFoot.transform.rotation = Quaternion.Euler(0f, transform.rotation.eulerAngles.y, 0f);
      rightFoot.transform.rotation = Quaternion.Euler(0f, transform.rotation.eulerAngles.y, 0f);

      // solve the FABRIK algorithm for each leg
      while (!allTargets_areWithinRange()) {
        leftLeg.Solve(transform.position);
        rightLeg.Solve(transform.position);

        // break if over the current iterations
        if (iter > maxIters) { return; }
        iter += 1;
      }
    }

    private bool allTargets_areWithinRange()
    {
      return leftLeg.DistanceIsWithinTolerance
          && rightLeg.DistanceIsWithinTolerance;
    }

    // ****************************************************************
    //    FOOTSTEPS
    // ****************************************************************
    private void updateFootsteps()
    {
      switch (currentSteps) {
        case foot_placement.STANDING:
          if (!leftFootProximityOK()) {
            // initialize
            initializeLeftFootStep();
            // call update step
            updateLeftStep();
            // set state
            currentSteps = foot_placement.LEFT_FOOT_STEPPING;
          } else if (!rightFootProximityOK()) {
            // initialize
            initializeRightFootStep();
            // call update
            updateRightStep();
            // set state
            currentSteps = foot_placement.RIGHT_FOOT_STEPPING;
          }
          break;
        case foot_placement.LEFT_FOOT_STEPPING:
          updateLeftStep();
          // test step time
          if ((Time.time - timeStepStarted) > stepMaxTime) {
            // foot has arrived, check right foot
            if (!rightFootProximityOK()) {
              initializeRightFootStep();
              currentSteps = foot_placement.RIGHT_FOOT_STEPPING;
            } else {
              currentSteps = foot_placement.STANDING;
            }
          }
          break;
        case foot_placement.RIGHT_FOOT_STEPPING:
          updateRightStep();
          // test step time
          if ((Time.time - timeStepStarted) > stepMaxTime) {
            // foot has arrived, check left foot
            if (!leftFootProximityOK()) {
              initializeLeftFootStep();
              currentSteps = foot_placement.LEFT_FOOT_STEPPING;
            } else {
              currentSteps = foot_placement.STANDING;
            }
          }
          break;
      }
    }

    private bool leftFootProximityOK()
    {
      float heightDelta = 1f - transform.localPosition.y;

      if ((leftFootWorldPosition - transform.TransformPoint(defaultLeftOffset + Vector3.up * heightDelta)).sqrMagnitude
                > (maxFootDelta * maxFootDelta)) {
        return false;
      }
      return true;
    }

    private bool rightFootProximityOK()
    {
      float heightDelta = 1f - transform.localPosition.y;

      if ((rightFootWorldPosition - transform.TransformPoint(defaultRightOffset + Vector3.up * heightDelta)).sqrMagnitude
                > (maxFootDelta * maxFootDelta)) {
        return false;
      }
      return true;
    }

    private void initializeLeftFootStep()
    {
      // initilize left foot step data
      leftFootStartPoint = leftFootWorldPosition;
      // get the relative direction for the footfall
      var target = transform.TransformPoint(defaultLeftOffset) + mechCurrentVelocity * stepMaxTime / 2f;
      var direction = target - transform.position;

      // build raycast data
      var ray = new Ray(transform.position, direction * 2f);
      int layerMask = 1 << 8;
      // perform raycast
      if (Physics.Raycast(ray, out var hit, 2f, layerMask)) {
        // we've hit terrain, move foot point here
        leftFootDestination = hit.point + Vector3.up * footHeight;
      } else {
        // no hit, place foot in a default location
        leftFootDestination = transform.position + direction;
      }

      timeStepStarted = Time.time;
    }

    private void initializeRightFootStep()
    {
      // initialize right foot destination data
      rightFootStartPoint = rightFootWorldPosition;

      // get the relative direction for the footfall
      var target = transform.TransformPoint(defaultRightOffset) + mechCurrentVelocity * stepMaxTime / 2f;
      var direction = target - transform.position;

      // build raycast data
      var ray = new Ray(transform.position, direction * 2f);
      int layerMask = 1 << 8;
      // perform raycast
      if (Physics.Raycast(ray, out var hit, 2f, layerMask)) {
        // we've hit terrain, move foot point here
        rightFootDestination = hit.point + Vector3.up * footHeight;
      } else {
        // no hit, place foot in a default location
        rightFootDestination = transform.position + direction;
      }

      timeStepStarted = Time.time;
    }

    private void updateLeftStep()
    {
      // bezier path
      leftFootWorldPosition = SimpleSpline.MoveAlong3PointCurve(
        leftFootStartPoint,
        leftFootDestination,
        leftFootDestination + Vector3.up * 0.2f,
        (Time.time - timeStepStarted) / stepMaxTime
      );
    }

    private void updateRightStep()
    {
      // bezier path
      rightFootWorldPosition = SimpleSpline.MoveAlong3PointCurve(
        rightFootStartPoint,
        rightFootDestination,
        rightFootDestination + Vector3.up * 0.2f,
        (Time.time - timeStepStarted) / stepMaxTime
      );
    }
  }
}