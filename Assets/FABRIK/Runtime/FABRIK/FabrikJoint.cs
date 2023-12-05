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
    public enum PreferredUp { Up, Interpolate, Override }
    [SerializeField] protected PreferredUp preferredUp = PreferredUp.Up;
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
      SetupConicRestraints();
    }

    public void SetupConicRestraints()
    {
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
      switch (preferredUp) {
        case PreferredUp.Interpolate:
          var up = Vector3.Lerp(downchain.up, upchain.up, preferenceTowardsUpchain);
          transform.LookAt(lookAtPosition, up);
          break;
        case PreferredUp.Override:
          transform.LookAt(lookAtPosition, lookAtUpOverride);
          break;
        case PreferredUp.Up:
        default:
          transform.LookAt(lookAtPosition, Vector3.up);
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
      var globalDirection = newDownstream - transform.position;

      // we should use the diration from upchain->(this) to define the cone axis
      // if the transforms are too close, we instead just use the upchain forward
      // as the cone's central axis
      var dir = transform.position - upchain.transform.position;
      if (dir.sqrMagnitude < 0.1 * 0.1) {
        dir = upchain.forward;
      }

      var rota = Quaternion.LookRotation(dir, transform.up);
      var pose = new Pose(transform.position, rota);

      // get the fraction of the global forward, projected onto the upchain's XY plane,
      // to determine what the local "height" (h) of the cone will be
      float h = Vector3.Dot(globalDirection, pose.forward);

      // get the projection of this point on the plane, for a plane with normal
      // equal to the upchain's forward vector
      var localProjection = Vector3.ProjectOnPlane(globalDirection, pose.forward);

      // consider local z-axis rotation (joint twist) and rotate our local projection by it
      float zRote = -transform.localEulerAngles.z;
      var rotation = Quaternion.AngleAxis(zRote, upchain.forward);
      localProjection = rotation * localProjection;

      // see if the projection is facing "backwards"
      bool inside90 = true;
      if (h < 0) {
        h = -h;
        // TODO finish the reverse case
        inside90 = false;
      }
      // get components (local to upchainTR) in global values
      float xPart = Vector3.Dot(localProjection, pose.right);
      float yPart = Vector3.Dot(localProjection, pose.up);

      // get the ratio of the cone's height, to the total downstream distance,
      // to determine by how much we scale the cone axes
      float scale = h / downstreamDistance;

      // determine a & b by checking which quadrant we're in, and scaling the appropriate
      // cone axes to reproduce the ellipse in this given quadrant
      var a = xPart > 0 ? coneRight * scale : -coneLeft * scale;
      var b = yPart > 0 ? coneTop * scale : -coneBot * scale;

      // test to see if the point is in bounds of the ellipse
      float ellipse = (xPart * xPart) / (a * a) + (yPart * yPart) / (b * b);
      if (ellipse > 1 || !inside90) {
        // out of bounds, and not on ellipse; find the closest point to the requested
        var closestPoint = solveEllipsePoint(a, b, xPart, yPart, h);

        // rotate to world space
        var rotated = pose.right * closestPoint.x + pose.up * closestPoint.y + pose.forward * closestPoint.z;
        // invert the z rotation applied from earlier
        rotated = Quaternion.AngleAxis(-zRote, pose.forward) * rotated;

        // add the rotated, scaled direction to adjusted
        var translated = rotated + transform.position;
        return translated;
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

      // get the scalar for this distance
      float delta = (transform.position - newPosition).magnitude;
      // normalized to the largest delta, scaled
      delta /= largestDelta;

      // adjust the global
      var constrainedPosition = Vector3.MoveTowards(newPosition, newPosition + d_spring * delta, d_1.magnitude);
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
      return adjusted;
    }
  }
}
