using UnityEngine;
using Yohash.Propulsion;

namespace Yohash.FABRIK
{
  public class PhysicalFabrikJoint : FabrikJoint
  {
    [Header("Rotational PD controller")]
    [SerializeField] private bool applyRotationForce = false;
    [SerializeField] private BackwardsPdController rotator;
    [SerializeField] private Vector3 torque;

    [Header("Translation PD controller")]
    [SerializeField] private bool applyTranslationForce = false;
    [SerializeField] private bool matchVelocity = false;
    [SerializeField] private BackwardsPdController translator;
    [SerializeField] private Vector3 throttle;

    [Header("Internal vars")]
    [SerializeField] private Vector3 target;
    [SerializeField] private float delta;
    [SerializeField] private Vector3 targetLast;
    [SerializeField] private Vector3 lookAtPosition;

    public bool SHOW_FORCE_DIR = false;
    public bool SHOW_TARGET_DIR = false;

    private Rigidbody rb;

    public override void AssignPosition(Vector3 position)
    {
      target = position;
    }

    public override void LookAtUp(Vector3 up)
    {
      _lookAtUp = up;
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
      throttle = matchVelocity
        ? translator.UpdatePosition(dt, transform.position, target, rb.velocity, targetVelocity)
        : translator.UpdatePosition(dt, transform.position, target, rb.velocity);

      targetLast = target;

      // determine rotation directions
      var lookDir = lookAtPosition - transform.position;
      var quat = Quaternion.LookRotation(lookDir, _lookAtUp);

      // solve rotational torque needed to meet to look requirements using
      // the stable backwards PD controller
      torque = rotator.UpdateRotation(
        dt,
        quat,
        transform.rotation,
        rb.angularVelocity,
        rb.inertiaTensorRotation,
        rb.inertiaTensor
      );

      // finally, apply the forces
      if (applyTranslationForce) {
        rb.AddForce(throttle);
      }
      if (applyRotationForce) {
        rb.AddTorque(torque);
      }
    }

    private void LateUpdate()
    {
      if (SHOW_FORCE_DIR) {
        Debug.DrawRay(transform.position, throttle.normalized * 0.25f, Color.yellow);
      }
      if (SHOW_TARGET_DIR) {
        Debug.DrawRay(transform.position, target - transform.position, Color.cyan);
      }

      delta = (target - transform.position).magnitude;
    }
  }
}
