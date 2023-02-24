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
    [SerializeField] private PidRotationController rotator;

    [Header("Final values")]
    [SerializeField] private Vector3 throttle;
    [SerializeField] private Vector3 torque;

    [Header("Internal vars")]
    [SerializeField] private Vector3 target;
    [SerializeField] private Vector3 lookAt;

    private Rigidbody rb;

    public override void AssignPosition(Vector3 position)
    {
      target = position;
    }

    public override void LookAt(Vector3 lookAtPosition)
    {
      lookAt = lookAtPosition;
    }

    private void FixedUpdate()
    {
      if (rb == null) { rb = GetComponent<Rigidbody>(); }

      float dt = Time.fixedDeltaTime;
      // solve translational motion
      throttle = new Vector3(
        controllerX.Update(dt, rb.position.x, target.x),
        controllerY.Update(dt, rb.position.y, target.y),
        controllerZ.Update(dt, rb.position.z, target.z)
      );

      // solve rotational motion
      torque = rotator.Update(
        dt,
        new Pose(transform.position, transform.rotation),
        lookAt,
        Vector3.up
      );

      // apply the forces
      rb.AddForce(throttle * Power);
      rb.AddRelativeTorque(torque * RotationalPower);
    }
  }
}
