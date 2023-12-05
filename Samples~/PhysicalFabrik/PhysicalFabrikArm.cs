using UnityEngine;

namespace Yohash.FABRIK.Samples.Physics.Arm
{
  public class PhysicalFabrikArm : MonoBehaviour
  {
    [SerializeField] private FabrikChain chain;
    [SerializeField] private Transform target;

    private void FixedUpdate()
    {
      if (chain.Target == null || chain.Target != target) {
        chain.Target = target;
      }

      if (chain.Target == null) { return; }

      chain.Solve();
    }
  }
}
