using UnityEngine;

namespace Yohash.FABRIK
{
  public interface IJoint
  {
    Transform Transform { get; }
    float JointWeight { get; }
    float UpstreamDistance { get; }
    float DownstreamDistance { get; }
    void SetupDownstream(IJoint joint);
    void SetupUpstream(Transform joint);
    Vector3 ConstrainPoint(Vector3 newGlobalPosition, Vector3 oldGlobalPosition);
    void AssignPosition(Vector3 position);
    void LookAtPosition(Vector3 lookAtPosition);
    void LookAtUp(Vector3 localUpDirection);
  }
}
