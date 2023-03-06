﻿using UnityEngine;

namespace Yohash.FABRIK
{
  public class FabrikJoint : MonoBehaviour, IJoint
  {
    [Header("Define joint characteristics")]
    [Range(0, 1)]
    [SerializeField] private float jointWeight = 1f;

    [Header("Define Conic Rotational constraints")]
    [SerializeField] private bool constrainRotation = false;
    // rotate clockise about Y (Y=up), move right
    [Range(0, 189)]
    [SerializeField] private float roteRight = 89f;
    // rotate counter-clockise about Y (Y=up), move left
    [Range(-189, 0)]
    [SerializeField] private float roteLeft = -89f;
    // rotate clockise about X (X=right), move down
    [Range(0, 189)]
    [SerializeField] private float roteUp = 89f;
    // rotate counter-clockwise about X (X=right), move up
    [Range(-189, 0)]
    [SerializeField] private float roteDown = -89f;

    [Header("Define Preferred Direction")]
    [SerializeField] private bool hasPreferredDirection = false;
    [SerializeField] private Vector3 preferredRelativeForward;

    [Range(0, 0.9f)]
    [SerializeField] private float springAlpha = 0.3f;

    [Header("Define Preferred LookAt Direction")]
    [SerializeField] private bool hasLookAt_PreferredDirection = false;
    [SerializeField] private Transform lookAt_PreferredTransform;
    [SerializeField] private Vector3 lookAt_PreferredRelativeVector = Vector3.forward;

    [Header("Cached chain data")]
    [SerializeField] private Transform upchain;
    [SerializeField] protected Vector3 _lookAtUp = Vector3.up;

    private Vector3 startOffset;
    private float linkLength;
    private float _startOffsetDistance;

    // compute the distance of the axes of each conic section
    private float coneTop;
    private float coneBot;
    private float coneRight;
    private float coneLeft;

    private float largestDelta;

    // TODO - offload all debugging stuff into an editor script
    [Header("DEBUG")]
    public bool DEBUG_SHOWCONE;
    public bool DEBUG_SHOWDIR;
    public bool DEBUG_SHOWUPSTRM;
    public bool DEBUG_SHOWPREF;

    // ****************************************************************
    //		IJoint
    // ****************************************************************
    public Transform Transform => transform;
    public float JointWeight => jointWeight;
    public float StartOffsetDistance => _startOffsetDistance;

    public void SetupDownstream(IJoint downstream)
    {
      linkLength = downstream.StartOffsetDistance;
      // precompute the cone axes lengths
      coneTop = linkLength * Mathf.Tan(roteUp * Mathf.Deg2Rad);
      coneBot = linkLength * Mathf.Tan(roteDown * Mathf.Deg2Rad);
      coneRight = linkLength * Mathf.Tan(roteRight * Mathf.Deg2Rad);
      coneLeft = linkLength * Mathf.Tan(roteLeft * Mathf.Deg2Rad);
      largestDelta = Mathf.Max(Mathf.Abs(coneTop) + Mathf.Abs(coneBot), Mathf.Abs(coneLeft) + Mathf.Abs(coneRight));
    }

    public void SetupUpstream(Transform upstream)
    {
      upchain = upstream;
      // precompute offsets
      startOffset = upstream.position - transform.position;
      _startOffsetDistance = startOffset.magnitude;
      preferredRelativeForward.Normalize();
    }

    public Vector3 ConstrainPoint(Vector3 newGlobalPosition, Vector3 oldGlobalPosition)
    {
      var final = newGlobalPosition;

      // first, we see if this joint has constrained movement, and refit the desired position to
      // the outisde of the conic section in our plane of movement
      if (constrainRotation) {
        final = applyRotationalConstraints(final);
      }

      // next, we see if this joint has a preferred relative position, and further constrain
      // the point to prefer this relative position
      if (hasPreferredDirection) {
        final = applyPreferredDirectionConstraints(final, oldGlobalPosition);
      }

      return final;
    }

    public virtual void AssignPosition(Vector3 position)
    {
      transform.position = position;
    }

    public virtual void LookAtPosition(Vector3 lookAtPosition)
    {
      if (hasLookAt_PreferredDirection && lookAt_PreferredTransform != null) {
        transform.LookAt(lookAtPosition, lookAt_PreferredTransform.TransformDirection(lookAt_PreferredRelativeVector));
      } else {
        transform.LookAt(lookAtPosition);
      }
    }

    public virtual void LookAtUp(Vector3 up)
    {
      _lookAtUp = up;
    }

    // ****************************************************************
    //		MONOBEHAVIOURS
    // ****************************************************************
    void Update()
    {
      if (DEBUG_SHOWDIR) {
        Debug.DrawRay(transform.position, transform.right, Color.black);
        Debug.DrawRay(transform.position, transform.up, Color.black);
        Debug.DrawRay(transform.position, transform.forward, Color.black);
      }
      if (DEBUG_SHOWUPSTRM) {
        Debug.DrawRay(upchain.position, upchain.right, Color.gray);
        Debug.DrawRay(upchain.position, upchain.up, Color.gray);
        Debug.DrawRay(upchain.position, upchain.forward, Color.gray);
      }
    }

    // ********************************************************************************************************************************
    //		FABRIK Joint functions
    // ********************************************************************************************************************************
    private Vector3 applyRotationalConstraints(Vector3 current)
    {
      // first off, we declare several important relevant variables
      // find the direction vector from this joint, to the new global position
      var globalDirection = current - transform.position;

      // get the ratio of the respective heights and scale the cone to our current conic cross section
      float h = Vector3.Dot(globalDirection, upchain.forward);

      // get the projection of this point on the plane
      var globalProjection = Vector3.ProjectOnPlane(globalDirection, upchain.forward);
      bool inside90 = true;
      if (h < 0) {
        // out of bounds
        h = -h;
        // TODO finsih the reverse case
        inside90 = false;
      }
      // get components (local to upchainTR) in global values
      float xPart = Vector3.Dot(globalProjection, upchain.right);
      float yPart = Vector3.Dot(globalProjection, upchain.up);

      // scale length for conic cross section scalar
      float scale = Mathf.Abs(h) / linkLength;

      // determine quadrant
      var xBnd = xPart > 0 ? coneRight * scale : -coneLeft * scale;
      var yBnd = yPart > 0 ? coneTop * scale : -coneBot * scale;

      // test to see if the point is in bounds of the ellipse
      float ellipse = (xPart * xPart) / (xBnd * xBnd) + (yPart * yPart) / (yBnd * yBnd);


      if (DEBUG_SHOWCONE) {
        Debug.DrawRay(transform.position + (upchain.forward * h), globalProjection, Color.blue);
        Debug.DrawRay(transform.position, globalDirection, Color.yellow);
        Debug.DrawRay(transform.position, upchain.right * xPart, Color.magenta);
        Debug.DrawRay(transform.position, upchain.up * yPart, Color.magenta);

        for (float theta = 0; theta < Mathf.PI * 2f; theta += Mathf.PI / 25f) {
          var xp = Mathf.Sign(Mathf.Cos(theta));
          var yp = Mathf.Sign(Mathf.Sin(theta));

          var xb = xp > 0 ? coneRight * scale : -coneLeft * scale;
          var yb = yp > 0 ? coneTop * scale : -coneBot * scale;

          var dtx = (xb / scale) * Mathf.Cos(theta);
          var dty = (yb / scale) * Mathf.Sin(theta);

          Debug.DrawRay(transform.position, (upchain.forward * linkLength + (upchain.right * dtx + upchain.up * dty)).normalized * linkLength, Color.white);
        }
      }


      if (ellipse > 1 || !inside90) {
        // out of bounds, and not on ellipse; find the closest point to the requested
        return solveEllipsePoint(xBnd, yBnd, xPart, yPart, h);
      }

      return current;
    }

    private Vector3 applyPreferredDirectionConstraints(Vector3 current, Vector3 oldGlobalPosition)
    {
      // first off, we declare several important relevant variables
      // find the direction vector from this joint, to the new global position
      var globalDirection = current - transform.position;

      // get the ratio of the respective heights and scale the cone to our current conic cross section
      float h = Vector3.Dot(globalDirection, upchain.forward);

      // make sure we can get a solution to our problem first
      if (h <= 0) { return current; }

      // get the projection of this point on the plane
      var globalProjection = Vector3.ProjectOnPlane(globalDirection, upchain.forward);
      // only dampen if the point is facing front
      // get components (local to upchainTR) in global values
      //float xPart = Vector3.Dot (globalProjection, upchain.right);
      //float yPart = Vector3.Dot (globalProjection, upchain.up);

      // scale length for conic cross section scalar
      float scale = Mathf.Abs(h) / linkLength;

      // transform  our preferred direction to our current relative forward
      // and scale it to the length of the requested global
      var globalPreferred = upchain.TransformDirection(preferredRelativeForward * linkLength) * scale;

      // now, we need the projection of preferred Direction on the plane
      var globalPreferredProjection = Vector3.ProjectOnPlane(globalPreferred, upchain.forward);

      // get the vector between the two points, this is the distance on which spring constant
      // is enforced
      var d_1 = globalPreferredProjection - globalProjection;
      var d_spring = d_1 * springAlpha;

      // get the distance that the target traveled

      // get the scalar for this distance
      float delta = (oldGlobalPosition - current).magnitude;
      // normalized to the largest delta, scaled
      delta /= largestDelta;

      // adjust the global
      var newGlobal = current + d_spring * delta;

      if (DEBUG_SHOWPREF) {
        Debug.DrawRay(transform.position, upchain.forward * linkLength, Color.yellow);
        Debug.DrawRay(transform.position, globalPreferred, Color.red);
        Debug.DrawRay(transform.position, globalDirection, Color.blue);
        Debug.DrawRay(transform.position + upchain.forward * linkLength, globalProjection, Color.magenta);
        Debug.DrawRay(transform.position + upchain.forward * linkLength, globalPreferredProjection, Color.cyan);
        Debug.DrawRay(transform.position, (newGlobal - transform.position), Color.green);
      }

      return newGlobal;
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
      adjusted = adjusted.normalized * linkLength;
      // rotate to world space
      var rotated = upchain.right * adjusted.x + upchain.up * adjusted.y + upchain.forward * adjusted.z;
      // add the rotated, scaled direction to adjusted
      adjusted = rotated + transform.position;

      if (DEBUG_SHOWCONE && constrainRotation) {
        Debug.DrawLine(transform.position, adjusted, Color.red);
      }

      return adjusted;
    }
  }
}
