using UnityEngine;

namespace Yohash.FABRIK
{
  public class FabrikJoint : MonoBehaviour, IJoint
  {
    // Joint Characteristics
    [SerializeField] private float jointWeight = 1f;

    // Conic rotational constraints
    [SerializeField] private bool constrainRotation = false;
    [SerializeField] private float roteRight = 60;
    [SerializeField] private float roteLeft = -60;
    [SerializeField] private float roteUp = 60;
    [SerializeField] private float roteDown = -60;

    // define preferred forward (+z) direction
    [SerializeField] private bool hasPreferredDirection = false;
    [SerializeField] private Vector3 preferredRelativeForward = Vector3.forward;
    [SerializeField] private float preferredDirectionStrength = 0.3f;

    // Define preferred up-facing technique
    public enum PreferredUp { None, Interpolate, Override }
    [SerializeField] protected PreferredUp hasPreferredUp = PreferredUp.None;
    [SerializeField] protected Vector3 lookAtUpOverride = Vector3.up;
    [SerializeField] protected float preferenceTowardsUpchain = 0.5f;

    // Misc cached variables
    [SerializeField] protected Transform upchain;
    [SerializeField] protected Transform downchain;
    // put in defaults here just so some math doesn't result in 0-value
    // joint values causing divide-by-0 errors
    [SerializeField] private float downstreamDistance = 1f;
    [SerializeField] private float upstreamDistance = 1f;

    // compute the distance of the axes of each conic section
    private float coneTop;
    private float coneBot;
    private float coneRight;
    private float coneLeft;
    private float largestDelta;

    // ****************************************************************
    //		IJoint
    // ****************************************************************
    public Transform Transform => transform;
    public float JointWeight => jointWeight;
    public float UpstreamDistance => upstreamDistance;
    public float DownstreamDistance => downstreamDistance;

    public void SetupDownstream(IJoint downstream)
    {
      downstreamDistance = downstream.UpstreamDistance;
      downchain = downstream.Transform;
      // precompute the cone axes lengths
      coneTop = downstreamDistance * Mathf.Tan(roteUp * Mathf.Deg2Rad);
      coneBot = downstreamDistance * Mathf.Tan(roteDown * Mathf.Deg2Rad);
      coneRight = downstreamDistance * Mathf.Tan(roteRight * Mathf.Deg2Rad);
      coneLeft = downstreamDistance * Mathf.Tan(roteLeft * Mathf.Deg2Rad);
      largestDelta = Mathf.Max(Mathf.Abs(coneTop) + Mathf.Abs(coneBot), Mathf.Abs(coneLeft) + Mathf.Abs(coneRight));
    }

    public void SetupUpstream(Transform upstream)
    {
      upchain = upstream;
      // precompute offsets
      var upstreamOffset = upstream.position - transform.position;
      upstreamDistance = upstreamOffset.magnitude;
      preferredRelativeForward.Normalize();
    }

    public Vector3 ConstrainDownstreamPoint(Vector3 newDownstreamPosition)
    {
      var constrainedDownstream = newDownstreamPosition;

      // next, we see if this joint has a preferred relative position, and further constrain
      // the point to prefer this relative position
      if (hasPreferredDirection) {
        constrainedDownstream = applyPreferredDirection(constrainedDownstream);
      }

      // first, we see if this joint has constrained movement, and refit the desired position to
      // the outisde of the conic section in our plane of movement
      if (constrainRotation) {
        constrainedDownstream = constrainToCone(constrainedDownstream);
      }

      return constrainedDownstream;
    }

    public virtual void AssignPosition(Vector3 position)
    {
      transform.position = position;
    }

    public virtual void LookAtPosition(Vector3 lookAtPosition)
    {
      switch (hasPreferredUp) {
        case PreferredUp.Interpolate:
          var up = Vector3.Lerp(downchain.up, upchain.up, preferenceTowardsUpchain);
          transform.LookAt(lookAtPosition, up);
          break;
        case PreferredUp.Override:
          transform.LookAt(lookAtPosition, lookAtUpOverride);
          break;
        case PreferredUp.None:
        default:
          transform.LookAt(lookAtPosition);
          break;
      }
    }

    public void LookAtUp(Vector3 up)
    {
      lookAtUpOverride = up;
    }

    // ********************************************************************************************************************************
    //		FABRIK Joint functions
    // ********************************************************************************************************************************
    private Vector3 constrainToCone(Vector3 newDownstream)
    {
      // first off, we declare several important relevant variables
      // find the direction vector from this joint, to the new position
      var newDirection = newDownstream - transform.position;

      // get the ratio of the respective heights and scale the cone to
      // our current conic cross section
      float h = Vector3.Dot(newDirection, upchain.forward);

      // get the projection of this point on the plane, for a plane with normal
      // equal to the upchain's forward vector
      var planarProjection = Vector3.ProjectOnPlane(newDirection, upchain.forward);
      bool inside90 = true;
      if (h < 0) {
        // the projection is facing "backwards"
        h = -h;
        // TODO finish the reverse case
        inside90 = false;
      }
      // get components (local to upchainTR) in global values
      float xPart = Vector3.Dot(planarProjection, upchain.right);
      float yPart = Vector3.Dot(planarProjection, upchain.up);

      // scale length for conic cross section scalar
      float scale = h / downstreamDistance;

      // determine quadrant
      var xBnd = xPart > 0 ? coneRight * scale : -coneLeft * scale;
      var yBnd = yPart > 0 ? coneTop * scale : -coneBot * scale;

      // test to see if the point is in bounds of the ellipse
      float ellipse = (xPart * xPart) / (xBnd * xBnd) + (yPart * yPart) / (yBnd * yBnd);
      if (ellipse > 1 || !inside90) {
        // out of bounds, and not on ellipse; find the closest point to the requested
        return solveEllipsePoint(xBnd, yBnd, xPart, yPart, h);
      }

      return newDownstream;
    }

    private Vector3 applyPreferredDirection(Vector3 newPosition)
    {
      // first off, we declare several important relevant variables
      // find the direction vector from this joint, to the new global position
      var newDirection = newPosition - transform.position;

      // get the ratio of the respective heights and scale the cone to our current conic cross section
      float h = Vector3.Dot(newDirection, upchain.forward);

      // make sure we can get a solution to our problem first
      if (h <= 0) { return newPosition; }

      // get the projection of this point on the plane
      var planarProjection = Vector3.ProjectOnPlane(newDirection, upchain.forward);

      // scale length for conic cross section scalar
      float scale = Mathf.Abs(h) / downstreamDistance;

      // transform  our preferred direction to our current relative forward
      // and scale it to the length of the requested global
      var globalPreferred = upchain.TransformDirection(preferredRelativeForward * downstreamDistance) * scale;

      // now, we need the projection of preferred Direction on the plane
      var globalPreferredProjection = Vector3.ProjectOnPlane(globalPreferred, upchain.forward);

      // get the vector between the two points, this is the distance on which spring constant
      // is enforced
      var d_1 = globalPreferredProjection - planarProjection;
      var d_spring = d_1 * preferredDirectionStrength;

      // get the distance that the target traveled

      // get the scalar for this distance
      float delta = (transform.position - newPosition).magnitude;
      // normalized to the largest delta, scaled
      delta /= largestDelta;

      // adjust the global
      var constrainedPosition = newPosition + d_spring * delta;
      return constrainedPosition;
    }

    private Vector3 solveEllipsePoint(float a, float b, float x0, float y0, float h)
    {
      // TECHNIQUE: use an iterative robust root finding technique to find the nearest
      // point on the ellipse, to the given point (x0,y0) = (xpart, ypart);
      var v2 = Ellipse.MinPoint_OnEllipse_FromPoint(Mathf.Abs(a), Mathf.Abs(b), Mathf.Abs(x0), Mathf.Abs(y0));

      var dx = v2.x * Mathf.Sign(x0);
      var dy = v2.y * Mathf.Sign(y0);

      // convert point to 3d space
      var adjusted = new Vector3(dx, dy, h);
      // extend the length of this link
      adjusted = adjusted.normalized * downstreamDistance;
      // rotate to world space
      var rotated = upchain.right * adjusted.x + upchain.up * adjusted.y + upchain.forward * adjusted.z;
      // add the rotated, scaled direction to adjusted
      adjusted = rotated + transform.position;
      return adjusted;
    }
  }
}
