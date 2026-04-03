using UnityEngine;

[ExecuteAlways]
public class MeshScatterExclusionZone : MonoBehaviour
{
    public enum ZoneShape
    {
        Box,
        Sphere
    }

    public ZoneShape shape = ZoneShape.Box;
    public Vector3 boxSize = new Vector3(10f, 5f, 10f);
    public float sphereRadius = 5f;

    public bool Contains(Vector3 worldPoint)
    {
        if (shape == ZoneShape.Box)
        {
            Vector3 localPoint = transform.InverseTransformPoint(worldPoint);
            Vector3 half = boxSize * 0.5f;

            return Mathf.Abs(localPoint.x) <= half.x &&
                   Mathf.Abs(localPoint.y) <= half.y &&
                   Mathf.Abs(localPoint.z) <= half.z;
        }

        return Vector3.Distance(transform.position, worldPoint) <= sphereRadius;
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = new Color(1f, 0.2f, 0.2f, 0.35f);
        Matrix4x4 oldMatrix = Gizmos.matrix;

        if (shape == ZoneShape.Box)
        {
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawCube(Vector3.zero, boxSize);
            Gizmos.color = Color.red;
            Gizmos.DrawWireCube(Vector3.zero, boxSize);
        }
        else
        {
            Gizmos.DrawSphere(transform.position, sphereRadius);
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, sphereRadius);
        }

        Gizmos.matrix = oldMatrix;
    }
}