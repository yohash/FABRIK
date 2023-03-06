using UnityEngine;
using Yohash.Propulsion;

namespace Yohash.FABRIK
{
  public class PhysicalFabrikJoint : FabrikJoint
  {
    [Header("Declare engine power")]
    [SerializeField] private float Power = 10f;
    [SerializeField] private float RotationalPower = 50f;

    [Header("3-axis PID controllers")]
    [SerializeField] private PidController controllerX;
    [SerializeField] private PidController controllerY;
    [SerializeField] private PidController controllerZ;

    [Header("3-axis rotational PID controller")]
    [SerializeField] private bool ApplyRotationForce = false;
    [SerializeField] private BackwardsPdController rotator;

    [Header("Final values")]
    [SerializeField] private Vector3 throttle;
    [SerializeField] private Vector3 torque;

    [Header("Internal vars")]
    [SerializeField] private Vector3 target;
    [SerializeField] private Vector3 lookAtPosition;

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

      // solve for translation motion, and store the force in Throttle
      throttle = new Vector3(
        controllerX.Update(dt, transform.position.x, target.x),
        controllerY.Update(dt, transform.position.y, target.y),
        controllerZ.Update(dt, transform.position.z, target.z)
      );

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
      rb.AddForce(throttle * Power);
      if (ApplyRotationForce) {
        rb.AddTorque(torque * RotationalPower);
      }
    }
  }
}
