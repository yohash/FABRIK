using UnityEngine;

namespace Yohash.FABRIK
{
  public class ConeGizmo : MonoBehaviour
  {
    public float AngleRight = 1.0f;
    public float LeftAngle = 1.0f;
    public float UpAngle = 1.0f;
    public float DownAngle = 1.0f;
    public float Length = 0.15f;

    private void OnDrawGizmos()
    {
      var mesh = ConeMeshGenerator.CreateConeMesh(
        AngleRight,
        LeftAngle,
        UpAngle,
        DownAngle,
        Length,
        0,
        90
      );

      Gizmos.color = new Color(1, 1, 0, 1);
      Gizmos.DrawWireMesh(mesh, transform.position, transform.rotation);
    }
  }

  public class LineGizmo : MonoBehaviour
  {
    public Vector3 PreferredForward;
    public float Length = 0.2f;

    private void OnDrawGizmos()
    {
      Gizmos.color = Color.blue;
      Gizmos.DrawLine(
        transform.position,
        transform.position + transform.TransformDirection(PreferredForward).normalized * Length
      );
    }
  }

  public class MatchingArrowGizmo : MonoBehaviour
  {
    public Vector3 RelativeUp;
    public float Length = 0.2f;

    private void OnDrawGizmos()
    {
      Gizmos.color = Color.red;
      Gizmos.DrawLine(
        transform.position,
        transform.position + transform.TransformDirection(RelativeUp).normalized * Length
      );

      Gizmos.color = Color.cyan;
      Gizmos.DrawLine(
        transform.position,
        transform.position + transform.up * Length
      ); ;
    }
  }

  public class FabrikChainSolutionGizmo : MonoBehaviour
  {
    public Vector3[] Solution;

    private void OnDrawGizmos()
    {
      if (Solution == null || Solution.Length == 0) { return; }
      for (int i = 1; i < Solution.Length; i++) {
        Gizmos.DrawLine(Solution[i], Solution[i - 1]);
      }
    }
  }

  public static class ConeMeshGenerator
  {
    /// <summary>
    /// Quad: (0: all), (1: up-left), (2: up-right), (3: down-right), (4: down-left)
    /// </summary>
    /// <param name="degreesRight"></param>
    /// <param name="degreesLeft"></param>
    /// <param name="degreesUp"></param>
    /// <param name="degreesDown"></param>
    /// <param name="height"></param>
    /// <param name="quad"></param>
    /// <param name="steps"></param>
    /// <returns></returns>
    public static Mesh CreateConeMesh(
      float degreesRight,
      float degreesLeft,
      float degreesUp,
      float degreesDown,
      float height,
      int quad = 0,
      int steps = 16
    )
    {
      var coneMesh = new Mesh();

      int numVertices = steps + 1;
      var vertices = new Vector3[numVertices + 1];
      var uv = new Vector2[numVertices + 1];
      int[] triangles = new int[steps * 3 * 2];

      // Define the apex vertex
      vertices[0] = new Vector3(0, 0, 0);

      // Define UV coordinates for apex
      uv[0] = new Vector2(0.5f, 1);

      float min =
        quad == 2 ? 90
        : quad == 3 ? 180
        : quad == 4 ? 270
        : 0;
      float max =
        quad == 1 ? 90
        : quad == 2 ? 180
        : quad == 3 ? 270
        : 360;

      var angleStep = (max - min) / steps;
      //var quat = Quaternion.FromToRotation(axis, desiredAxis);

      Vector3 GetVert(int i)
      {
        float angle = (min + angleStep * i) * Mathf.Deg2Rad;

        var coneTop = height * Mathf.Tan(degreesUp * Mathf.Deg2Rad);
        var coneBot = height * Mathf.Tan(degreesDown * Mathf.Deg2Rad);
        var coneRight = height * Mathf.Tan(degreesRight * Mathf.Deg2Rad);
        var coneLeft = height * Mathf.Tan(degreesLeft * Mathf.Deg2Rad);

        var xSign = Mathf.Sign(Mathf.Cos(angle));
        var ySign = Mathf.Sign(Mathf.Sin(angle));

        var a = xSign > 0 ? coneRight : -coneLeft;
        var b = ySign > 0 ? coneTop : -coneBot;

        var x = a * Mathf.Cos(angle);
        var y = b * Mathf.Sin(angle);

        var vector = new Vector3(x, y, height).normalized * height;

        //return quat * vector;
        return vector;
      }

      // Generate vertices and UV coordinates for the open edge
      for (int i = 0; i < steps; i++) {
        vertices[i + 1] = GetVert(i);
        uv[i + 1] = new Vector2(0.5f, 0.5f);

        int currentTriangleIndex = i * 3 * 2;
        // Apex to open edge triangles
        triangles[currentTriangleIndex] = 0;
        triangles[currentTriangleIndex + 1] = (i + 1) % (steps + 1) + 1;
        triangles[currentTriangleIndex + 2] = i + 1;
        // backface
        triangles[currentTriangleIndex + 3] = 0;
        triangles[currentTriangleIndex + 4] = i + 1;
        triangles[currentTriangleIndex + 5] = (i + 1) % (steps + 1) + 1;
      }

      vertices[vertices.Length - 1] = GetVert(steps);
      uv[uv.Length - 1] = new Vector2(0.5f, 0.5f);

      coneMesh.vertices = vertices;
      coneMesh.uv = uv;
      coneMesh.triangles = triangles;
      coneMesh.RecalculateNormals();

      return coneMesh;
    }
  }
}