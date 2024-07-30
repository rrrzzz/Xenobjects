using EasyButtons;
using UnityEngine;

[ExecuteAlways]
public class RecenterMesh : MonoBehaviour
{
    [Button]
    public void CenterMeshes()
    {
        MeshFilter meshFilter = GetComponent<MeshFilter>();
        Mesh mesh = meshFilter.sharedMesh;

        // Store the original world position
        Vector3 originalWorldPosition = transform.position;

        // Calculate the current center in local space
        Vector3 localCenter = mesh.bounds.center;

        // Calculate the current center in world space
        Vector3 worldCenter = transform.TransformPoint(localCenter);

        Vector3[] vertices = mesh.vertices;
        for (int i = 0; i < vertices.Length; i++)
        {
            vertices[i] -= localCenter;
        }
        

        mesh.vertices = vertices;
        mesh.RecalculateBounds();

        // Adjust the object's local position to compensate
        transform.localPosition += transform.InverseTransformVector(worldCenter - originalWorldPosition);
    }
}