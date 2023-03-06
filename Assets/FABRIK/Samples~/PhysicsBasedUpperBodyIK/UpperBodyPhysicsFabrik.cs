using UnityEngine;
using Yohash.Propulsion;

namespace Yohash.FABRIK
{
  /// <summary>
  /// Upper torso FABRIK.
  ///
  /// Designed specifically for a torso with two arms.
  /// Optionally a head may be declared, and if so, will contribute
  /// to the rotation of the torso.
  /// </summary>
  public class UpperBodyPhysicsFabrik : MonoBehaviour
  {
    // this script is placed on the central hub
    [SerializeField] private int maxIters = 10;

    // The left and right arm FRABRIK Chains
    [Header("Left and Right Arm Chains")]
    [SerializeField] private FabrikChain leftArm;
    [SerializeField] private FabrikChain rightArm;

    // for the 'head'
    [Header("Head")]
    [SerializeField] private Transform headObjectTransform;
    [SerializeField] private Transform headBaseOffsetReference;
    [SerializeField] private float headVerticalOffset = -0.35f;
    // cached variables
    private Quaternion lastHeadRotation;

    [Header("Declare engine power")]
    [SerializeField] private float Power = 10f;
    [SerializeField] private float RotationalPower = 50f;

    [Header("3-axis PID controllers")]
    [SerializeField] private PidController controllerX;
    [SerializeField] private PidController controllerY;
    [SerializeField] private PidController controllerZ;

    [Header("Torso rotation controller")]
    [SerializeField] private BackwardsPdController rotator;

    [Header("Movement Force values")]
    [SerializeField] private Vector3 throttle;
    [SerializeField] private Vector3 torque;

    // cache the look at direction
    private Vector3 lookAt;

    // cache the torse rigidbody
    private Rigidbody torso;

    private void Awake()
    {
      if (torso == null) {
        torso = GetComponent<Rigidbody>();
      }
    }

    void Start()
    {
      var pose = transform.ToPose();
      leftArm.Intialize(pose);
      rightArm.Intialize(pose);
    }

    private void Update()
    {
      SolveIK();
    }

    private void FixedUpdate()
    {
      moveTorso();
    }

    public void SolveIK()
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

        // using this position as the root, perform a forward pass
        // perform a forwards pass over all chains
        leftArm.Forward(transform.position);
        rightArm.Forward(transform.position);
        // physically move the chains
        leftArm.Move();
        rightArm.Move();

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
      // Place the torso center directly below the head-tracker
      var newTorsoPosition = headBaseOffsetReference.position + Vector3.up * headVerticalOffset;

      float dt = Time.fixedDeltaTime;
      // solve for translation motion, and store the force in Throttle
      throttle = new Vector3(
        controllerX.Update(dt, transform.position.x, newTorsoPosition.x),
        controllerY.Update(dt, transform.position.y, newTorsoPosition.y),
        controllerZ.Update(dt, transform.position.z, newTorsoPosition.z)
      );

      torso.AddForce(throttle * Power);
      // transform.position = newTorsoPosition;

      lookAt = Vector3.zero;
      // adjust the rotation to face in the averaged relative forward vectors
      lookAt += leftArm.SecondLink.TransformDirection(leftArm.LocalRelativeForward);
      lookAt += rightArm.SecondLink.TransformDirection(rightArm.LocalRelativeForward);

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
         transform.rotation,
         torso.angularVelocity,
         torso.inertiaTensorRotation,
         torso.inertiaTensor
       );

      torso.AddTorque(torque * RotationalPower);
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
