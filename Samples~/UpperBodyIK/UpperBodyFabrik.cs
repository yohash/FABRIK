using UnityEngine;

namespace Yohash.FABRIK
{
  /// <summary>
  /// Upper torso FABRIK.
  ///
  /// Designed specifically for a torso with two arms.
  /// Optionally a head may be declared, and if so, will contribute
  /// to the rotation of the torso.
  /// </summary>
  public class UpperBodyFabrik : MonoBehaviour
  {
    // this script is placed on the central hub
    [SerializeField] private int maxIters = 10;

    // The left and right arm FRABRIK Chains
    [Header("Left and Right Arm Chains")]
    [SerializeField] private FabrikChain leftArm;
    [SerializeField] private FabrikChain rightArm;

    // if we want to override any axis of the torso's rotation
    [Header("Torso Rotation Overrides")]
    [SerializeField] private bool xAxisRotationIgnore;
    [SerializeField] private bool yAxisRotationIgnore;
    [SerializeField] private bool zAxisRotationIgnore;

    // for the 'head'
    [Header("Head")]
    [SerializeField] private Transform headObjectTransform;
    [SerializeField] private Transform headBaseOffsetReference;
    [SerializeField] private float headVerticalOffset = -0.35f;

    // cached variables
    private Quaternion lastHeadRotation;

    void Start()
    {
      var pose = transform.ToPose();
      leftArm.Intialize(pose);
      rightArm.Intialize(pose);
    }

    private void Update()
    {
      moveTorso();
      solveIK();
    }

    // ****************************************************************
    //    PUBLIC ACCESSORS
    // ****************************************************************
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

        // using this position as the root, perform a forward pass
        // perform a forwards pass over all chains
        leftArm.Forward();
        rightArm.Forward();
        // physically move the chains
        leftArm.Move();
        rightArm.Move();

        // check current iterations
        if (iter > maxIters) { return; }
        iter += 1;
      }
    }

    /// <summary>
    /// Adjusts the hub rotation.
    /// </summary>
    private void moveTorso()
    {
      // Place the torso center directly below the head-tracker
      var newTorsoPosition = headBaseOffsetReference.position + Vector3.up * headVerticalOffset;
      transform.position = newTorsoPosition;

      var lookAt = Vector3.zero;
      // adjust the rotation to face in the averaged relative forward vectors
      var leftContribution = leftArm.SecondLink.TransformDirection(leftArm.LocalRelativeForward);
      var rightContribution = rightArm.SecondLink.TransformDirection(rightArm.LocalRelativeForward);

      lookAt += leftContribution;
      lookAt += rightContribution;

      // determine if we tilt further forward or not
      float downQuotient = Vector3.Dot(Vector3.down, headObjectTransform.forward);
      if (downQuotient < 0) {
        // just use the half
        lookAt += headObjectTransform.forward / 2f;
      } else {
        // we are looking down, bend further
        lookAt += headObjectTransform.forward * (Mathf.Clamp(downQuotient * 2f, 0.5f, 2f));
      }

      // get quaternion to rotate our forward to said new forward
      if (lookAt != Vector3.zero) {
        var toQuat = Quaternion.LookRotation(lookAt);

        var roteEuler = toQuat.eulerAngles;

        // apply rotational axes filters
        if (xAxisRotationIgnore) { roteEuler.x = 0f; }
        if (yAxisRotationIgnore) { roteEuler.y = 0f; }
        if (zAxisRotationIgnore) { roteEuler.z = 0f; }

        toQuat = Quaternion.Euler(roteEuler);
        transform.rotation = toQuat;
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
      bool withinRange = leftArm.IsWithinTolerance && rightArm.IsWithinTolerance;
      // now check the head object if it is declared
      if (headObjectTransform != null && lastHeadRotation != headObjectTransform.rotation) {
        withinRange = false;
      }

      return withinRange;
    }
  }
}
