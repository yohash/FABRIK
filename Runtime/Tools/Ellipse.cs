using UnityEngine;

public static class Ellipse
{
  /// <summary>
  /// The ellipse solver.
  /// adapted from: https://www.geometrictools.com/Documentation/DistancePointEllipseEllipsoid.pdf
  ///
  /// </summary>
  private static int maxIterations = 10;

  /// <summary>
  /// Uses a robust minimizer to to iteratively find the closest point on an
  /// ellipse to a given point.
  /// </summary>
  /// <returns>The point on ellipse the shortest distance from point.</returns>
  /// <param name="a">x-axis of the ellipse.</param>
  /// <param name="b">y-axis of the ellipse.</param>
  /// <param name="x0">x0 = x component of input point</param>
  /// <param name="y0">y0 = y component of input point</param>
  public static Vector2 MinPoint_OnEllipse_FromPoint(float a, float b, float x0, float y0)
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
    var v = swap ? new Vector2(y1, x1) : new Vector2(x1, y1);
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
  private static float getRoot(float r0, float z0, float z1, float g)
  {
    // robust root finder
    float n0 = r0 * z0;
    float s0 = z1 - 1;

    // s1 is the magnitude of this vector
    float s1 = (new Vector2(n0, z1)).magnitude - 1;
    if (g < 0) {
      s1 = 0;
    }

    float s = 0;

    for (int i = 0; i < maxIterations; i++) {
      s = (s0 + s1) / 2f;
      if (s == s0 || s == s1) {
        break;
      }

      float ratio0 = n0 / (s + r0);
      float ratio1 = z1 / (s + 1);

      g = (ratio0 * ratio0) + (ratio1 * ratio1) - 1f;

      if (g > 0) {
        s0 = s;
      } else if (g < 0) {
        s1 = s;
      } else {
        break;
      }
    }

    return s;
  }
}