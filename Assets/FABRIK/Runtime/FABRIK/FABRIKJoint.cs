using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FABRIKJoint : MonoBehaviour
{
  public Transform myTR;

  public Vector3 startOffset;
  public float startOffsetDistance;

  public float linkLength;

  [Header("Conic Rotational constraints")]
  public bool applyConstraints = false;
  [Range(0, 189)]
  // rotate clockise about Y (Y=up), move right
  public float roteRight = 89f;
  [Range(-189, 0)]
  // rotate counter-clockise about Y (Y=up), move left
  public float roteLeft = -89f;
  [Range(0, 189)]
  // rotate clockise about X (X=right), move down
  public float roteUp = 89f;
  [Range(-189, 0)]
  // rotate counter-clockwise about X (X=right), move up
  public float roteDown = -89f;

  public Transform upchainTR;
  [Range(0, 1)]
  public float jointWeight = 1f;

  [Header("Preferred Direction")]
  public bool hasPreferredDirection = false;

  public Vector3 preferredRelativeForward;
  public Vector3 preferredActualForward;

  [Range(0, 0.9f)]
  public float springAlpha = 0.3f;

  [Header("Preferred LookAt Direction")]
  public bool hasLookAt_PreferredDirection = false;
  public Transform lookAt_PreferredTransform;
  public Vector3 lookAt_PreferredRelativeVector = Vector3.forward;

  private float coneTop;
  private float coneBot;
  private float coneRight;
  private float coneLeft;

  private float largestDelta;

  [Header("DEBUG")]
  public bool DEBUG_SHOWCONE;
  public bool DEBUG_SHOWDIR;
  public bool DEBUG_SHOWUPSTRM;
  public bool DEBUG_SHOWPREF;

  // ****************************************************************
  //		MONOBEHAVIOURS
  // ****************************************************************
  void Awake()
  {
    myTR = transform;

    startOffset = myTR.localPosition;

    startOffsetDistance = startOffset.magnitude;

    preferredRelativeForward.Normalize();
  }

  void Update()
  {
    if (DEBUG_SHOWDIR) {
      Debug.DrawRay(myTR.position, myTR.right, Color.black);
      Debug.DrawRay(myTR.position, myTR.up, Color.black);
      Debug.DrawRay(myTR.position, myTR.forward, Color.black);
    }
    if (DEBUG_SHOWUPSTRM) {
      Debug.DrawRay(upchainTR.position, upchainTR.right, Color.gray);
      Debug.DrawRay(upchainTR.position, upchainTR.up, Color.gray);
      Debug.DrawRay(upchainTR.position, upchainTR.forward, Color.gray);
    }
  }

  // ****************************************************************
  //    PUBLIC ACCESSOR
  // ****************************************************************
  public void LookAt_NextJoint(Vector3 worldPosition)
  {
    if (hasLookAt_PreferredDirection && lookAt_PreferredTransform != null) {
      myTR.LookAt(worldPosition, lookAt_PreferredTransform.TransformDirection(lookAt_PreferredRelativeVector));
    } else {
      myTR.LookAt(worldPosition);
    }
  }

  // ****************************************************************
  //    necessary inputs for constrained movements
  // ****************************************************************
  public void setupFABRIKChain(Transform fj)
  {
    upchainTR = fj;

    setup_ConeConstants();
  }

  public Vector3 constrainPoint(Vector3 newGlobalPosition, Vector3 oldGlobalPosition)
  {
    Vector3 modGlobalPosition = newGlobalPosition;
    // first, we see if this joint has constrained movement, and refit the desired position to
    // the outisde of the conic section in our plane of movement
    if (applyConstraints) {
      // first off, we declare several important relevant variables
      // find the direction vector from this joint, to the new global position
      Vector3 globalDirection = modGlobalPosition - myTR.position;

      // get the ratio of the respective heights and scale the cone to our current conic cross section
      float h = Vector3.Dot(globalDirection, upchainTR.forward);

      // get the projection of this point on the plane
      Vector3 globalProjection = Vector3.ProjectOnPlane(globalDirection, upchainTR.forward);
      bool inside90 = true;
      if (h < 0) {
        // out of bounds
        h = -h;
        // TODO finsih the reverse case
        inside90 = false;
      }
      // get components (local to upchainTR) in global values
      float xPart = Vector3.Dot(globalProjection, upchainTR.right);
      float yPart = Vector3.Dot(globalProjection, upchainTR.up);

      // scale length for conic cross section scalar
      float scale = Mathf.Abs(h) / linkLength;

      // determine quadrant
      float xBnd, yBnd;
      if (xPart > 0)
        xBnd = coneRight * scale;
      else
        xBnd = -coneLeft * scale;

      if (yPart > 0)
        yBnd = coneTop * scale;
      else
        yBnd = -coneBot * scale;

      // test to see if the point is in bounds of the ellipse
      float ellipse = (xPart * xPart) / (xBnd * xBnd) + (yPart * yPart) / (yBnd * yBnd);

      if (ellipse > 1 || !inside90) {
        // out of bounds, and not on ellipse; find the closest point to the requested
        modGlobalPosition = solveEllipsePoint(xBnd, yBnd, xPart, yPart, h);
      }

      if (DEBUG_SHOWCONE) {
        Debug.DrawRay(myTR.position + (upchainTR.forward * h), globalProjection, Color.blue);
        Debug.DrawRay(myTR.position, globalDirection, Color.yellow);
        Debug.DrawRay(myTR.position, upchainTR.right * xPart, Color.magenta);
        Debug.DrawRay(myTR.position, upchainTR.up * yPart, Color.magenta);

        for (float theta = 0; theta < Mathf.PI * 2f; theta += Mathf.PI / 25f) {
          float xp, yp;
          xp = Mathf.Sign(Mathf.Cos(theta));
          yp = Mathf.Sign(Mathf.Sin(theta));

          float xb, yb;
          if (xp > 0)
            xb = coneRight * scale;
          else
            xb = -coneLeft * scale;
          if (yp > 0)
            yb = coneTop * scale;
          else
            yb = -coneBot * scale;

          float dtx, dty;
          dtx = (xb / scale) * Mathf.Cos(theta);
          dty = (yb / scale) * Mathf.Sin(theta);
          Debug.DrawRay(myTR.position, (upchainTR.forward * linkLength + (upchainTR.right * dtx + upchainTR.up * dty)).normalized * linkLength, Color.white);
        }
      }
    }

    // next, we see if this joint has a preferred relative position, and further constrain
    // the point to prefer this relative position
    if (hasPreferredDirection) {
      // first off, we declare several important relevant variables
      // find the direction vector from this joint, to the new global position
      Vector3 globalDirection = modGlobalPosition - myTR.position;

      // get the ratio of the respective heights and scale the cone to our current conic cross section
      float h = Vector3.Dot(globalDirection, upchainTR.forward);

      // make sure we can get a solution to our problem first
      if (h > 0) {
        // get the projection of this point on the plane
        Vector3 globalProjection = Vector3.ProjectOnPlane(globalDirection, upchainTR.forward);
        // only dampen if the point is facing front
        // get components (local to upchainTR) in global values
        //float xPart = Vector3.Dot (globalProjection, upchainTR.right);
        //float yPart = Vector3.Dot (globalProjection, upchainTR.up);

        // scale length for conic cross section scalar
        float scale = Mathf.Abs(h) / linkLength;

        // transform  our preferred direction to our current relative forward
        // and scale it to the length of the requested global
        Vector3 globalPreferred = upchainTR.TransformDirection(preferredActualForward) * scale;

        // now, we need the projection of preferred Direction on the plane
        Vector3 globalPreferredProjection = Vector3.ProjectOnPlane(globalPreferred, upchainTR.forward);

        // get the vector between the two points, this is the distance on which spring constant
        // is enforced
        Vector3 d_1 = globalPreferredProjection - globalProjection;
        Vector3 d_spring = d_1 * springAlpha;

        // get the distance that the target traveled

        // get the scalar for this distance
        float delta = (oldGlobalPosition - modGlobalPosition).magnitude;
        // normalized to the largest delta, scaled
        delta /= (largestDelta);

        // adjust the global
        Vector3 tempGlob = modGlobalPosition + d_spring * delta;
        modGlobalPosition = tempGlob;

        if (DEBUG_SHOWPREF) {
          Debug.DrawRay(myTR.position, upchainTR.forward * linkLength, Color.yellow);
          Debug.DrawRay(myTR.position, globalPreferred, Color.red);
          Debug.DrawRay(myTR.position, globalDirection, Color.blue);
          Debug.DrawRay(myTR.position + upchainTR.forward * linkLength, globalProjection, Color.magenta);
          Debug.DrawRay(myTR.position + upchainTR.forward * linkLength, globalPreferredProjection, Color.cyan);
          Debug.DrawRay(myTR.position, (tempGlob - myTR.position), Color.green);
        }
      }
    }

    return modGlobalPosition;
  }

  // ********************************************************************************************************************************
  //		FABRIK Joint functions
  // ********************************************************************************************************************************
  private Vector3 solveEllipsePoint(float a, float b, float x0, float y0, float h)
  {
    // out of bounds, and not on ellipse; find the closest point (dx, dy)
    float dx = 0f, dy = 0f;
    // cache variables
    Vector3 adjusted;
    //Quaternion quat;
    Vector3 rotated;
    // TECHNIQUE: use an iterative robust root finding technique to find the nearest
    // point on the ellipse, to the given point (x0,y0) = (xpart, ypart);
    Vector2 v2 = minPoint_onEllipse_fromPoint(Mathf.Abs(a), Mathf.Abs(b), Mathf.Abs(x0), Mathf.Abs(y0));

    dx = v2.x * Mathf.Sign(x0);
    dy = v2.y * Mathf.Sign(y0);

    // convert point to 3d space
    adjusted = new Vector3(dx, dy, h);
    // extend the length of this link
    adjusted = adjusted.normalized * linkLength;
    // rotate to world space
    rotated = upchainTR.right * adjusted.x + upchainTR.up * adjusted.y + upchainTR.forward * adjusted.z;
    // add the rotated, scaled direction to adjusted
    adjusted = rotated + myTR.position;

    if (DEBUG_SHOWCONE && applyConstraints) {
      Debug.DrawLine(myTR.position, adjusted, Color.red);
    }

    // ************** 	END TECHNIQUEs    **************
    return adjusted;
  }

  private void setup_ConeConstants()
  {
    coneTop = linkLength * Mathf.Tan(roteUp * Mathf.Deg2Rad);
    coneBot = linkLength * Mathf.Tan(roteDown * Mathf.Deg2Rad);
    coneRight = linkLength * Mathf.Tan(roteRight * Mathf.Deg2Rad);
    coneLeft = linkLength * Mathf.Tan(roteLeft * Mathf.Deg2Rad);

    largestDelta = Mathf.Max((Mathf.Abs(coneTop) + Mathf.Abs(coneBot)), (Mathf.Abs(coneLeft) + Mathf.Abs(coneRight)));
  }



  /// <summary>
  /// The ellipse solver.
  /// adapted from: https://www.geometrictools.com/Documentation/DistancePointEllipseEllipsoid.pdf
  ///
  /// </summary>
  private int maxIterations = 10;

  /// <summary>
  /// Uses a robust minimizer to to iteratively find the closest point on an
  /// ellipse to a given point.
  /// </summary>
  /// <returns>The point on ellipse the shortest distance from point.</returns>
  /// <param name="a">x-axis of the ellipse.</param>
  /// <param name="b">y-axis of the ellipse.</param>
  /// <param name="x0">x0 = x component of input point</param>
  /// <param name="y0">y0 = y component of input point</param>
  private Vector2 minPoint_onEllipse_fromPoint(float a, float b, float x0, float y0)
  {
    // must have a>b, if (b>a) we have to swap variables
    bool swap = false;
    if (b > a) {
      // we have to swap variables
      swap = true;
      // swap a and b
      float tmp = a;
      a = b;
      b = tmp;
      // swap x0 and y0
      tmp = x0;
      x0 = y0;
      y0 = tmp;
    }

    // the new point
    float x1, y1;
    float g;

    if (y0 > 0) {
      if (x0 > 0) {
        // pre-compute some ratios in the allipse equation
        float z0 = x0 / a;
        float z1 = y0 / b;
        // compute the ellipse equation
        g = z0 * z0 + z1 * z1 - 1;
        // check for point placement on/off ellipse
        if (g != 0) {
          // the point is outside/inside of the ellipse
          float r0 = (a / b) * (a / b);
          float sbar = getRoot(r0, z0, z1, g);
          // compute the final (x1, y1) from the root
          x1 = r0 * x0 / (sbar + r0);
          y1 = y0 / (sbar + 1);

        } else {
          // in this case, the point is on the ellipse
          x1 = x0;
          y1 = y0;
        }
      } else {
        // x0 == 0; y is the b-point
        x1 = 0;
        y1 = b;
      }
    } else {
      float numer0 = a * x0;
      float denom0 = a * a - b * b;

      if (numer0 < denom0) {
        float xde0 = numer0 / denom0;
        x1 = a * xde0;
        y1 = b * Mathf.Sqrt(1 - xde0 * xde0);
      } else {
        // y0 == 0, x is the a-point
        x1 = a;
        y1 = 0;
      }
    }

    // see if we had to swap axes before computation
    Vector2 v;
    if (swap) {
      // swap x1 and y1 in our returned vector
      v = new Vector2(y1, x1);
    } else {
      // assemble the final vector2 containing the nearest point on the ellipse
      v = new Vector2(x1, y1);
    }
    return v;
  }

  /// <summary>
  /// Robust Root Finder, adapted from: https://www.geometrictools.com/Documentation/DistancePointEllipseEllipsoid.pdf
  ///
  /// </summary>
  /// <returns>The root.</returns>
  /// <param name="r0">r0 = ratio of end-termina (a/b)</param>
  /// <param name="z0">z0 = ratio of x-term in ellipse eq (x/a).</param>
  /// <param name="z1">z1 = ratio of y-term in ellipse eq (y/b).</param>
  /// <param name="g">g is the value of the ellipse equation</param>
  private float getRoot(float r0, float z0, float z1, float g)
  {
    // robust root finder
    float n0 = r0 * z0;
    float s0 = z1 - 1;

    // s1 is the magnitude of this vector
    float s1 = (new Vector2(n0, z1)).magnitude - 1;
    if (g < 0)
      s1 = 0;

    float s = 0;

    for (int i = 0; i < maxIterations; i++) {
      s = (s0 + s1) / 2f;
      if (s == s0 || s == s1) {
        break;
      }

      float ratio0 = n0 / (s + r0);
      float ratio1 = z1 / (s + 1);

      g = (ratio0 * ratio0) + (ratio1 * ratio1) - 1f;

      if (g > 0)
        s0 = s;
      else if (g < 0)
        s1 = s;
      else
        break;
    }

    return s;
  }
}
