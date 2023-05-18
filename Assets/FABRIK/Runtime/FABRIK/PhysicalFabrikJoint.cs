using UnityEngine;
using Yohash.Propulsion;

namespace Yohash.FABRIK
{
  public class PhysicalFabrikJoint : FabrikJoint
  {
    // Rotation PD controller
    [SerializeField] private bool applyRotationForce = false;
    [SerializeField] private BackwardsPdController rotator;
    [SerializeField] private Vector3 outputTorque;

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

    public override void LookAtUp(Vector3 up)
    {
      lookAtUpOverride = up;
    }

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

      // determine rotation directions
      var lookDir = lookAtPosition - transform.position;
      var quat = Quaternion.LookRotation(lookDir, lookAtUpOverride);

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
