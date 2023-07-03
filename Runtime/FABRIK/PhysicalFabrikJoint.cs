using UnityEngine;
using Yohash.Propulsion;

namespace Yohash.FABRIK
{
  public class PhysicalFabrikJoint : FabrikJoint
  {
    public enum RotationMode { TwistOnly, EndEffector }
    // Rotation PD controller
    [SerializeField] private bool applyRotationForce = false;
    [SerializeField] private BackwardsPdController rotator;
    [SerializeField] private Vector3 outputTorque;
    [SerializeField] private RotationMode rotationMode = RotationMode.TwistOnly;

    // Translation PD controller
    [SerializeField] private bool applyTranslationForce = false;
    [SerializeField] private bool matchVelocity = false;
    [SerializeField] private BackwardsPdController translator;
    [SerializeField] private Vector3 outputThrottle;

    [Header("Internal vars")]
    [SerializeField] private Vector3 target;
    [SerializeField] private Vector3 targetLast;
    [SerializeField] private Vector3 lookAtPosition;

    private Rigidbody rb;

    public override void AssignPosition(Vector3 position)
    {
      target = position;
    }

    /// <summary>
    /// TODO - better way to do this might be to extend IJoint with PhysicalFabrikJoint
    ///         instead of piggy-backing off of FabrikJoint.
    ///        Offload FabrikJoint's classes to helper functions in a static class.
    /// </summary>
    public override void LookAtPosition(Vector3 lookAtPosition)
    {
      this.lookAtPosition = lookAtPosition;
    }

    private void FixedUpdate()
    {
      if (rb == null) { rb = GetComponent<Rigidbody>(); }

      float dt = Time.fixedDeltaTime;

      var targetVelocity = (target - targetLast) / Time.fixedDeltaTime;
      outputThrottle = matchVelocity
        ? translator.UpdatePosition(dt, transform.position, target, rb.velocity, targetVelocity)
        : translator.UpdatePosition(dt, transform.position, target, rb.velocity);

      targetLast = target;

      // determine rotation directions but only rotate along the z-axis (forward facing),
      // otherwise the rotator will try to rotate the joint in all directions to match the
      // new forward facing, producing a really janky rotation that fights against the
      // translational motor. If we're an end effector ("hand"), we'll want to look at the
      // direction prescribed
      var forward = transform.forward;
      if (rotationMode == RotationMode.EndEffector) {
        forward = lookAtPosition - transform.position;
      }

      var quat = Quaternion.identity;
      switch (preferredUp) {
        case PreferredUp.Interpolate:
          var up = Vector3.Lerp(downchain.up, upchain.up, preferenceTowardsUpchain);
          quat = Quaternion.LookRotation(forward, up);
          break;
        case PreferredUp.Override:
          quat = Quaternion.LookRotation(forward, lookAtUpOverride);
          break;
        case PreferredUp.Up:
        default:
          quat = Quaternion.LookRotation(forward, Vector3.up);
          break;
      }

      // solve rotational torque needed to meet to look requirements using
      // the stable backwards PD controller
      outputTorque = rotator.UpdateRotation(
        dt,
        quat,
        transform.rotation,
        rb.angularVelocity,
        rb.inertiaTensorRotation,
        rb.inertiaTensor
      );

      if (rotationMode == RotationMode.TwistOnly) {
        outputTorque = new Vector3(0, 0, outputTorque.z);
      }

      // finally, apply the forces
      if (applyTranslationForce) {
        rb.AddForce(outputThrottle);
      }
      if (applyRotationForce) {
        rb.AddTorque(outputTorque);
      }
    }
  }
}
