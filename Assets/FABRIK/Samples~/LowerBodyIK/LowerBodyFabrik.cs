﻿using UnityEngine;
using Yohash.Bezier;

namespace Yohash.FABRIK.Samples.Lowerbody
{
  public enum FootPlacement
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
    [SerializeField] private float footHeight = 0.1f;
    [SerializeField] private float maxFootDelta = 0.25f;
    [SerializeField] private float stepMaxTime = 1f;

    [SerializeField] private Vector3 defaultLeftOffset = new Vector3(-0.25f, -1f, 0f);
    [SerializeField] private Vector3 defaultRightOffset = new Vector3(0.25f, -1f, 0f);

    [Header("Tracked private vars")]
    [SerializeField] private Vector3 leftFootWorldPosition;
    [SerializeField] private Vector3 rightFootWorldPosition;
    [SerializeField] private FootPlacement currentSteps = FootPlacement.STANDING;

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
      transform.rotation = UpperTorso.rotation;

      // ensure both feet are postiioned at their world position
      leftFoot.transform.position = leftFootWorldPosition;
      rightFoot.transform.position = rightFootWorldPosition;

      // make sure both feet are aligned with the waist's forward
      leftFoot.transform.rotation = Quaternion.Euler(0f, transform.rotation.eulerAngles.y, 0f);
      rightFoot.transform.rotation = Quaternion.Euler(0f, transform.rotation.eulerAngles.y, 0f);

      // solve the FABRIK algorithm for each leg
      while (!allTargets_areWithinRange()) {
        leftLeg.Solve();
        rightLeg.Solve();

        // break if over the current iterations
        if (iter > maxIters) { return; }
        iter += 1;
      }
    }

    private bool allTargets_areWithinRange()
    {
      return leftLeg.IsWithinTolerance
          && rightLeg.IsWithinTolerance;
    }

    // ****************************************************************
    //    FOOTSTEPS
    // ****************************************************************
    private void updateFootsteps()
    {
      switch (currentSteps) {
        case FootPlacement.STANDING:
          if (!leftFootProximityOK()) {
            // initialize
            initializeLeftFootStep();
            // call update step
            updateLeftStep();
            // set state
            currentSteps = FootPlacement.LEFT_FOOT_STEPPING;
          } else if (!rightFootProximityOK()) {
            // initialize
            initializeRightFootStep();
            // call update
            updateRightStep();
            // set state
            currentSteps = FootPlacement.RIGHT_FOOT_STEPPING;
          }
          break;
        case FootPlacement.LEFT_FOOT_STEPPING:
          updateLeftStep();
          // test step time
          if ((Time.time - timeStepStarted) > stepMaxTime) {
            // foot has arrived, check right foot
            if (rightFootProximityOK()) {
              currentSteps = FootPlacement.STANDING;
            } else {
              initializeRightFootStep();
              currentSteps = FootPlacement.RIGHT_FOOT_STEPPING;
            }
          }
          break;
        case FootPlacement.RIGHT_FOOT_STEPPING:
          updateRightStep();
          // test step time
          if ((Time.time - timeStepStarted) > stepMaxTime) {
            // foot has arrived, check left foot
            if (leftFootProximityOK()) {
              currentSteps = FootPlacement.STANDING;
            } else {
              initializeLeftFootStep();
              currentSteps = FootPlacement.LEFT_FOOT_STEPPING;
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
      // get the relative direction for the footfall
      var target = transform.TransformPoint(defaultLeftOffset) + mechCurrentVelocity * stepMaxTime / 2f;
      var direction = target - transform.position;

      // initilize left foot data and store to locals
      leftFootStartPoint = leftFootWorldPosition;
      leftFootDestination = raycastToGround(direction);
      timeStepStarted = Time.time;
    }

    private void initializeRightFootStep()
    {
      // get the relative direction for the footfall
      var target = transform.TransformPoint(defaultRightOffset) + mechCurrentVelocity * stepMaxTime / 2f;
      var direction = target - transform.position;

      // initialize right foot data and store to locals
      rightFootStartPoint = rightFootWorldPosition;
      rightFootDestination = raycastToGround(direction);
      timeStepStarted = Time.time;
    }

    private Vector3 raycastToGround(Vector3 direction)
    {
      var ray = new Ray(transform.position, direction * 2f);
      if (Physics.Raycast(ray, out var hit)) {
        // we've hit something, move foot point here
        return hit.point + Vector3.up * footHeight;
      } else {
        // no hit, place foot in a default location
        return transform.position + direction;
      }
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
