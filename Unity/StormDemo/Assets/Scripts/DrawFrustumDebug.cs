using UnityEngine;

[ExecuteAlways]
public class DrawFrustumDebug : MonoBehaviour
{
    public Color lineColor = Color.green;

    void OnDrawGizmos()
    {
        Camera cam = GetComponent<Camera>();
        if (cam == null) return;

        Gizmos.color = lineColor;
        Matrix4x4 temp = Gizmos.matrix;

        Gizmos.matrix = Matrix4x4.TRS(transform.position, transform.rotation, Vector3.one);
        Gizmos.DrawFrustum(Vector3.zero, cam.fieldOfView, cam.farClipPlane, cam.nearClipPlane, cam.aspect);

        Gizmos.matrix = temp;
    }
}
