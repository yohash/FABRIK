using UnityEngine;
using Yohash.Propulsion;

namespace Yohash.FABRIK
{
  public class PhysicalFabrikJoint : FabrikJoint
  {
    [Header("Declare engine power")]
    [SerializeField] private float Power = 10f;

    [Header("3-axis PID controllers")]
    [SerializeField] private PIDController controllerX;
    [SerializeField] private PIDController controllerY;
    [SerializeField] private PIDController controllerZ;

    [Header("Final values")]
    [SerializeField] private Vector3 target;
    [SerializeField] private Vector3 throttle;

    private Rigidbody rb;

    //public override void AssignPosition(Vector3 position)
    //{
    //  target = position;
    //}

    //public override void LookAt(Vector3 lookAtPosition)
    //{
    //  // TODO - replace with physical form (add torque)
    //  transform.LookAt(lookAtPosition);
    //}

    private void FixedUpdate()
    {
      if (rb == null) { rb = GetComponent<Rigidbody>(); }

      throttle = new Vector3(
        controllerX.Update(Time.fixedDeltaTime, rb.position.x, target.x),
        controllerY.Update(Time.fixedDeltaTime, rb.position.y, target.y),
        controllerZ.Update(Time.fixedDeltaTime, rb.position.z, target.z)
      );

      rb.AddForce(throttle * Power);
    }
  }
}
