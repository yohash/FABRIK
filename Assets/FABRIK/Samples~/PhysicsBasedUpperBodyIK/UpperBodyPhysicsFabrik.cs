using UnityEngine;
using Yohash.Propulsion;

namespace Yohash.FABRIK.Samples.Physics.Upperbody
{
  /// <summary>
  /// Upper body physics-based FABRIK.
  ///
  /// Designed specifically for a torso with two arms.
  /// Optionally a head may be declared, and if so, will contribute
  /// to the rotation of the torso.
  /// </summary>
  public class UpperBodyPhysicsFabrik : MonoBehaviour
  {
    // this script is placed on the central hub
    [SerializeField] private int maxIters = 10;
    [SerializeField] private Transform torso;

    // The left and right arm FRABRIK Chains
    [Header("Left and Right Arm Chains")]
    [SerializeField] private FabrikChain leftArm;
    [SerializeField] private FabrikChain rightArm;
    [SerializeField] private FabrikChain head;

    // for the 'head'
    [Header("Head")]
    [SerializeField] private Transform headObjectTransform;
    // cached variables
    private Quaternion lastHeadRotation;

    [Header("Rotational PD controller")]
    [SerializeField] private bool applyRotationForce = false;
    [SerializeField] private BackwardsPdController rotator;
    [SerializeField] private Vector3 torque;

    [Header("Translation PD controller")]
    [SerializeField] private bool applyTranslationForce = false;
    [SerializeField] private BackwardsPdController translator;
    [SerializeField] private Vector3 throttle;

    // cache the look at direction
    private Vector3 lookAt;

    // cache the torse rigidbody
    private Rigidbody torsoRb;

    private void Awake()
    {
      if (torsoRb == null) {
        torsoRb = torso.GetComponent<Rigidbody>();
      }
    }

    private void FixedUpdate()
    {
      moveTorso();
      solveIK();
    }

    private void solveIK()
    {
      solve();

      if (headObjectTransform != null) {
        lastHeadRotation = headObjectTransform.rotation;
      }
    }

    // ****************************************************************
    //    solver
    // ****************************************************************
    private void solve()
    {
      // setup our iteration counter
      int iter = 0;

      // Check the two conditions that warrant IK updates
      //    1) the targets are not within the range tolerance
      //    2) a 'head' object is declared and it has rotated
      while (!allTargets_areWithinRange()) {
        // manually step through the FABRIK algorithm. After performing our backward
        // pass, we'll re-position the torso under the head base. Then, we'll call
        // the forward pass with this new torso position, and move the chain.
        leftArm.Backward();
        rightArm.Backward();
        head.Backward();

        // perform a forwards pass over all chains
        leftArm.Forward();
        rightArm.Forward();
        head.Forward();

        // physically move the chains
        leftArm.Move();
        rightArm.Move();
        head.Move();

        // check current iterations
        if (iter > maxIters) { return; }
        iter += 1;
      }
    }

    /// <summary>
    /// Adjusts the hub/torso.
    /// </summary>
    private void moveTorso()
    {
      float dt = Time.fixedDeltaTime;

      // Place the torso center directly below the head-tracker
      var newTorsoPosition = head.Target.position - Vector3.up * head.ChainLength * 0.9f;

      throttle = translator.UpdatePosition(
        dt,
        torso.position,
        newTorsoPosition,
        torsoRb.velocity
      );

      if (applyTranslationForce) {
        torsoRb.AddForce(throttle);
      }

      lookAt = Vector3.zero;
      if (leftArm.Positions.Count > 2) {
        // adjust the rotation to face in the averaged relative forward vectors
        var leftShoulderFacing = leftArm.Positions[2] - leftArm.Positions[1];
        var leftContribution = -Vector3.Cross(leftShoulderFacing, Vector3.up);
        var rightShoulderFacing = rightArm.Positions[2] - rightArm.Positions[1];
        var rightContribution = Vector3.Cross(rightShoulderFacing, Vector3.up);
        lookAt += leftContribution;
        lookAt += rightContribution;
      }
      // determine if we tilt further forward or not
      float downQuotient = Vector3.Dot(Vector3.down, headObjectTransform.forward);
      if (downQuotient < 0) {
        // just use the half
        lookAt += headObjectTransform.forward / 2f;
      } else {
        // we are looking down, bend further
        lookAt += headObjectTransform.forward * Mathf.Clamp(downQuotient * 2f, 0.5f, 2f);
      }

      // determine rotation directions
      var quat = Quaternion.LookRotation(lookAt, Vector3.up);
      // solve for the desired torso rotation using the stable backwards PD rotation controller
      torque = rotator.UpdateRotation(
         dt,
         quat,
         torsoRb.rotation,
         torsoRb.angularVelocity,
         torsoRb.inertiaTensorRotation,
         torsoRb.inertiaTensor
       );

      if (applyRotationForce) {
        torsoRb.AddTorque(torque);
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
      bool withinRange = leftArm.IsWithinTolerance
          && rightArm.IsWithinTolerance;
      // now check the head object if it is declared
      if (headObjectTransform != null && lastHeadRotation != headObjectTransform.rotation) {
        withinRange = false;
      }

      return withinRange;
    }
  }
}
