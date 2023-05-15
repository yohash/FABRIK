using UnityEngine;

namespace Yohash.FABRIK
{
  public class ConeGizmo : MonoBehaviour
  {
    public float PosX = 1.0f;
    public float NegX = 1.0f;
    public float PosY = 1.0f;
    public float NegY = 1.0f;

    private void OnDrawGizmos()
    {
      var mesh = ConeMeshGenerator.CreateConeMesh(PosX, NegX, PosY, NegY, 1, 0, 80);
      Gizmos.color = new Color(1, 1, 0, 1);
      Gizmos.DrawWireMesh(mesh, transform.position, transform.rotation);
    }
  }

  public class LineGizmo : MonoBehaviour
  {
    public Vector3 RelativeForward;

    private void OnDrawGizmos()
    {
      Gizmos.color = Color.red;
      Gizmos.DrawLine(
        transform.position,
        transform.position + transform.TransformDirection(RelativeForward).normalized
      );
    }
  }

  public class MatchingArrowGizmo : MonoBehaviour
  {
    public Vector3 RelativeUp;

    private void OnDrawGizmos()
    {
      Gizmos.color = Color.red;
      Gizmos.DrawLine(
        transform.position,
        transform.position + transform.TransformDirection(RelativeUp).normalized
      );

      Gizmos.color = Color.cyan;
      Gizmos.DrawLine(
        transform.position,
        transform.position + transform.up
      ); ;
    }
  }

  public static class ConeMeshGenerator
  {
    /// <summary>
    /// Quad: (0: all), (1: up-left), (2: up-right), (3: down-right), (4: down-left)
    /// </summary>
    /// <param name="px"></param>
    /// <param name="nx"></param>
    /// <param name="py"></param>
    /// <param name="ny"></param>
    /// <param name="height"></param>
    /// <param name="quad"></param>
    /// <param name="steps"></param>
    /// <returns></returns>
    public static Mesh CreateConeMesh(float px, float nx, float py, float ny, float height, int quad = 0, int steps = 16)
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

      Vector3 GetVert(int i)
      {
        float angle = (min + angleStep * i) * Mathf.Deg2Rad;

        var xp = Mathf.Sign(Mathf.Cos(angle));
        var yp = Mathf.Sign(Mathf.Sin(angle));

        var coneTop = height * Mathf.Tan(py * Mathf.Deg2Rad);
        var coneBot = height * Mathf.Tan(ny * Mathf.Deg2Rad);
        var coneRight = height * Mathf.Tan(px * Mathf.Deg2Rad);
        var coneLeft = height * Mathf.Tan(nx * Mathf.Deg2Rad);

        var xb = xp > 0 ? coneRight : -coneLeft;
        var yb = yp > 0 ? coneTop : -coneBot;

        var dtx = xb * Mathf.Cos(angle);
        var dty = yb * Mathf.Sin(angle);

        return new Vector3(dtx, dty, 1).normalized * height;
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