using UnityEngine;

namespace Yohash.FABRIK
{
  public static class PoseExtensions
  {
    public static Pose ToPose(this Transform t)
    {
      return new Pose(t.position, t.rotation);
    }
  }
}
