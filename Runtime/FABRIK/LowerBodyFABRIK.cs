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

  public class LowerBodyFABRIK : MonoBehaviour
  {
    // this script is placed on the waist
    private Transform waist;
    public int maxIters = 10;

    // The left and right arm FRABRIK Chains
    [Header("Chains")]
    public FABRIKChain LeftLegFABRIKChain;
    public FABRIKChain RightLegFABRIKChain;

    [Header("Foot Data")]
    public GameObject LeftFoot;
    public GameObject RightFoot;

    public float FootHeight = 0.2f;

    public Vector3 LeftFootWorldPosition;
    public Vector3 RightFootWorldPosition;

    private Vector3 leftFootDestination, leftFootStartPoint;
    private Vector3 rightFootDestination, rightFootStartPoint;

    public foot_placement currentSteps = foot_placement.STANDING;

    public float MaxFootDelta = 0.25f;
    public float stepMaxTime = 1f;

    public Vector3 mechCurrentVelocity = Vector3.zero;

    private float timeStepStarted;

    public Vector3 DefaultLeftOffset = new Vector3(-0.25f, -1f, 0f);
    public Vector3 DefaultRightOffset = new Vector3(0.25f, -1f, 0f);

    [Header("Upper Torso Tracking Point")]
    public Transform UpperTorso;

    // ****************************************************************
    //    MONOBEHAVIOURS
    // ****************************************************************
    void Awake()
    {
      waist = transform;
    }

    void Start()
    {
      intializeFABRIKChain(LeftLegFABRIKChain);
      intializeFABRIKChain(RightLegFABRIKChain);
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
      waist.position = UpperTorso.position;

      // ensure both feet are postiioned at their world position
      LeftFoot.transform.position = LeftFootWorldPosition;
      RightFoot.transform.position = RightFootWorldPosition;

      // make sure both feet are aligned with the waist's forward
      LeftFoot.transform.rotation = Quaternion.Euler(0f, waist.rotation.eulerAngles.y, 0f);
      RightFoot.transform.rotation = Quaternion.Euler(0f, waist.rotation.eulerAngles.y, 0f);

      // Check the targets are not within the range tolerance
      while (!allTargets_areWithinRange()) {
        // perform a backwards pass over both chains
        LeftLegFABRIKChain.backward();
        RightLegFABRIKChain.backward();

        // using this position as the root, perform a forward pass
        // perform a forwards pass over all chains
        LeftLegFABRIKChain.forward(waist.position);
        RightLegFABRIKChain.forward(waist.position);

        // physically move the chains
        LeftLegFABRIKChain.moveChain();
        RightLegFABRIKChain.moveChain();

        // check current iterations
        if (iter > maxIters)
          return;
        iter += 1;
      }
    }


    /// <summary>
    /// Determine if both arm targets are within range
    /// Two conditions warrant a target being not within range
    ///    1) the targets are not within the range tolerance
    ///    2) a 'head' object is declared and it has rotated
    /// </summary>
    /// <returns><c>true</c>, if all targets are within range, <c>false</c> otherwise.</returns>
    private bool allTargets_areWithinRange()
    {
      // initialize the return variable
      bool withinRange = true;
      // test both left and right arm
      withinRange = (withinRange && LeftLegFABRIKChain.targetDistance_isWithinTolerance());
      withinRange = (withinRange && RightLegFABRIKChain.targetDistance_isWithinTolerance());

      return withinRange;
    }

    /// <summary>
    /// Intializes the FABRIK chain by setting two necessary variables
    ///   (1) the local relative forward (used to center the hub)
    ///   (2) a reference to the central hub
    /// </summary>
    /// <param name="fabrikChain">Fabrik chain.</param>
    private void intializeFABRIKChain(FABRIKChain fabrikChain)
    {
      Transform joint = fabrikChain.chainSecond;

      float rt = Vector3.Dot(waist.forward, joint.right);
      float fwd = Vector3.Dot(waist.forward, joint.forward);
      float up = Vector3.Dot(waist.forward, joint.up);

      Vector3 v3 = new Vector3(rt, up, fwd);

      // set the two important variables on the FABRIK chain
      fabrikChain.myLocalRelativeForward = v3;
      fabrikChain.chainBase = waist;
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
      float heightDelta = (1f - waist.localPosition.y);

      if ((LeftFootWorldPosition - waist.TransformPoint(DefaultLeftOffset + Vector3.up * heightDelta)).sqrMagnitude
                > (MaxFootDelta * MaxFootDelta)) {
        return false;
      }
      return true;
    }
    private bool rightFootProximityOK()
    {
      float heightDelta = (1f - waist.localPosition.y);

      if ((RightFootWorldPosition - waist.TransformPoint(DefaultRightOffset + Vector3.up * heightDelta)).sqrMagnitude
                > (MaxFootDelta * MaxFootDelta)) {
        return false;
      }
      return true;
    }

    private void initializeLeftFootStep()
    {
      // initilize left foot step data
      leftFootStartPoint = LeftFootWorldPosition;
      // get the relative direction for the footfall
      Vector3 target = waist.TransformPoint(DefaultLeftOffset) + mechCurrentVelocity * stepMaxTime / 2f;
      Vector3 direction = target - waist.position;

      // build raycast data
      Ray ray = new Ray(waist.position, direction * 2f);
      int layerMask = 1 << 8;
      RaycastHit hit;
      // perform raycast
      if (Physics.Raycast(ray, out hit, 2f, layerMask)) {
        // we've hit terrain, move foot point here
        leftFootDestination = hit.point + Vector3.up * FootHeight;
      } else {
        // no hit, place foot in a default location
        leftFootDestination = waist.position + direction;
      }

      timeStepStarted = Time.time;
    }
    private void initializeRightFootStep()
    {
      // initialize right foot destination data
      rightFootStartPoint = RightFootWorldPosition;

      // get the relative direction for the footfall
      Vector3 target = waist.TransformPoint(DefaultRightOffset) + mechCurrentVelocity * stepMaxTime / 2f;
      Vector3 direction = target - waist.position;

      // build raycast data
      Ray ray = new Ray(waist.position, direction * 2f);
      int layerMask = 1 << 8;
      RaycastHit hit;
      // perform raycast
      if (Physics.Raycast(ray, out hit, 2f, layerMask)) {
        // we've hit terrain, move foot point here
        rightFootDestination = hit.point + Vector3.up * FootHeight;
      } else {
        // no hit, place foot in a default location
        rightFootDestination = waist.position + direction;
      }

      timeStepStarted = Time.time;
    }

    private void updateLeftStep()
    {
      // bezier path
      LeftFootWorldPosition = SimpleSpline.MoveAlong3PointCurve(
        leftFootStartPoint,
        leftFootDestination,
        leftFootDestination + Vector3.up * 0.2f,
        (Time.time - timeStepStarted) / stepMaxTime
      );
    }
    private void updateRightStep()
    {
      // bezier path
      RightFootWorldPosition = SimpleSpline.MoveAlong3PointCurve(
        rightFootStartPoint,
        rightFootDestination,
        rightFootDestination + Vector3.up * 0.2f,
        (Time.time - timeStepStarted) / stepMaxTime
      );
    }
  }
}