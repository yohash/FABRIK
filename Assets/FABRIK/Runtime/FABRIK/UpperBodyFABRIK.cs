using UnityEngine;

/// <summary>
/// Upper torso FABRIK.
///
/// Designed specifically for a torso with two arms.
/// Optionally a head may be declared, and if so, will contribute
/// to the rotation of the torso.
/// </summary>
public class UpperBodyFABRIK : MonoBehaviour
{
  // this script is placed on the central hub
  private Transform torso;
  public int maxIters = 10;

  // The left and right arm FRABRIK Chains
  [Header("Left and Right Arm Chains")]
  public FABRIKChain LeftArmFABRIKChain;
  public FABRIKChain RightArmFABRIKChain;

  // if we want to override any axis of the torso's rotation
  [Header("Torso Rotation Overrides")]
  public bool XAxisRotationIgnore;
  public bool YAxisRotationIgnore;
  public bool ZAxisRotationIgnore;

  // for the 'head'
  [Header("Head")]
  public Transform HeadObjectTransform;
  public Transform HeadBaseOffsetReference;
  public float HeadVerticalOffset = -0.35f;

  // cached variables
  private Quaternion lastHeadRotation;

  // ****************************************************************
  //    MONOBEHAVIOURS
  // ****************************************************************
  void Awake()
  {
    torso = transform;
  }

  void Start()
  {
    intializeFABRIKChain(LeftArmFABRIKChain);
    intializeFABRIKChain(RightArmFABRIKChain);
  }

  // ****************************************************************
  //    PUBLIC ACCESSORS
  // ****************************************************************
  public void SolveIK()
  {
    solve();

    if (HeadObjectTransform != null) {
      lastHeadRotation = HeadObjectTransform.rotation;
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
      // Place the torso center directly below the head-tracker
      Vector3 newTorsoPosition = HeadBaseOffsetReference.position + Vector3.up * HeadVerticalOffset;

      // perform a backwards pass over both chains
      LeftArmFABRIKChain.backward();
      RightArmFABRIKChain.backward();

      // move the torso after the backward phase is complete
      torso.position = newTorsoPosition;

      // adjust the rotation of the central hub at its new position
      adjustHubRotation();
      // using this position as the root, perform a forward pass
      // perform a forwards pass over all chains
      LeftArmFABRIKChain.forward(newTorsoPosition);
      RightArmFABRIKChain.forward(newTorsoPosition);
      // physically move the chains
      LeftArmFABRIKChain.moveChain();
      RightArmFABRIKChain.moveChain();

      // check current iterations
      if (iter > maxIters)
        return;
      iter += 1;
    }
  }

  /// <summary>
  /// Adjusts the hub rotation.
  /// </summary>
  private void adjustHubRotation()
  {
    Vector3 vn3 = Vector3.zero;
    // adjust the rotation to face in the averaged relative forward vectors
    vn3 += LeftArmFABRIKChain.ChainSecond.TransformDirection(LeftArmFABRIKChain.LocalRelativeForward);
    vn3 += RightArmFABRIKChain.ChainSecond.TransformDirection(RightArmFABRIKChain.LocalRelativeForward);

    // determine if we tilt further forward or not
    float downQuotient = Vector3.Dot(Vector3.down, HeadObjectTransform.forward);
    if (downQuotient < 0) {
      // just use the half
      vn3 += HeadObjectTransform.forward / 2f;
    } else {
      // we are looking down, bend further
      vn3 += HeadObjectTransform.forward * (Mathf.Clamp(downQuotient * 2f, 0.5f, 2f));
    }

    // get quaternion to rotate our forward to said new forward
    if (vn3 != Vector3.zero) {
      Quaternion toQuat = Quaternion.LookRotation(vn3);

      Vector3 roteEuler = toQuat.eulerAngles;

      // apply rotational axes filters
      if (XAxisRotationIgnore)
        roteEuler.x = 0f;
      if (YAxisRotationIgnore)
        roteEuler.y = 0f;
      if (ZAxisRotationIgnore)
        roteEuler.z = 0f;

      toQuat = Quaternion.Euler(roteEuler);

      torso.rotation = toQuat;
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
    withinRange = (withinRange && LeftArmFABRIKChain.DistanceIsWithinTolerance());
    withinRange = (withinRange && RightArmFABRIKChain.DistanceIsWithinTolerance());
    // now check the head object if it is declared
    if (HeadObjectTransform != null) {
      if (lastHeadRotation != HeadObjectTransform.rotation) {
        withinRange = false;
      }
    }

    return withinRange;
  }

  /// <summary>
  /// Intializes the FABRIK chain by setting two necessary variables
  ///
  ///   (1) the local relative forward (used to center the hub)
  ///   (2) a reference to the central hub
  ///
  /// </summary>
  /// <param name="fabrikChain">Fabrik chain.</param>
  private void intializeFABRIKChain(FABRIKChain fabrikChain)
  {
    Transform joint = fabrikChain.ChainSecond;

    float rt = Vector3.Dot(torso.forward, joint.right);
    float fwd = Vector3.Dot(torso.forward, joint.forward);
    float up = Vector3.Dot(torso.forward, joint.up);

    Vector3 v3 = new Vector3(rt, up, fwd);

    // set the two important variables on the FABRIK chain
    fabrikChain.LocalRelativeForward = v3;
    fabrikChain.ChainBase = torso;
  }
}
