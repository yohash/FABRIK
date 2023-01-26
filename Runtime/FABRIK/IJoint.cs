using UnityEngine;

namespace Yohash.FABRIK
{
  public interface IJoint
  {
    Transform Transform();
    float JointWeight();
    float StartOffsetDistance();
    void SetupDownstream(IJoint joint);
    void SetupUpstream(Transform joint);
    Vector3 ConstrainPoint(Vector3 newGlobalPosition, Vector3 oldGlobalPosition);
    void AssignPosition(Vector3 position);
    void LookAt(Vector3 lookAtPosition);
  }
}
