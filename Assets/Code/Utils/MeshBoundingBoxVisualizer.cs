using UnityEngine;

[ExecuteAlways]
[RequireComponent(typeof(MeshFilter))]
public class MeshBoundingBoxVisualizer : MonoBehaviour
{
    public Color boundingBoxColor = Color.green;
    public float lineWidth = 0.01f;

    private MeshFilter meshFilter;
    private Bounds bounds;
    private LineRenderer[] lineRenderers;

    void OnEnable()
    {
        meshFilter = GetComponent<MeshFilter>();
        bounds = meshFilter.sharedMesh.bounds;

        CreateBoundingBox();
    }

    void CreateBoundingBox()
    {
        Vector3[] corners = GetBoundingBoxCorners();
        int[][] lineConnections = new int[][]
        {
            new int[] {0, 1}, new int[] {1, 2}, new int[] {2, 3}, new int[] {3, 0}, // Bottom
            new int[] {4, 5}, new int[] {5, 6}, new int[] {6, 7}, new int[] {7, 4}, // Top
            new int[] {0, 4}, new int[] {1, 5}, new int[] {2, 6}, new int[] {3, 7}  // Sides
        };

        lineRenderers = new LineRenderer[lineConnections.Length];

        for (int i = 0; i < lineConnections.Length; i++)
        {
            GameObject lineObj = new GameObject($"BoundingBoxLine_{i}");
            lineObj.transform.SetParent(transform, false);
            LineRenderer lineRenderer = lineObj.AddComponent<LineRenderer>();

            lineRenderer.startColor = boundingBoxColor;
            lineRenderer.endColor = boundingBoxColor;
            lineRenderer.startWidth = lineWidth;
            lineRenderer.endWidth = lineWidth;
            lineRenderer.positionCount = 2;

            Vector3 startPoint = transform.TransformPoint(corners[lineConnections[i][0]]);
            Vector3 endPoint = transform.TransformPoint(corners[lineConnections[i][1]]);

            lineRenderer.SetPosition(0, startPoint);
            lineRenderer.SetPosition(1, endPoint);

            lineRenderers[i] = lineRenderer;
        }
    }

    Vector3[] GetBoundingBoxCorners()
    {
        Vector3 center = bounds.center;
        Vector3 extents = bounds.extents;

        return new Vector3[]
        {
            center + new Vector3(-extents.x, -extents.y, -extents.z),
            center + new Vector3(extents.x, -extents.y, -extents.z),
            center + new Vector3(extents.x, -extents.y, extents.z),
            center + new Vector3(-extents.x, -extents.y, extents.z),
            center + new Vector3(-extents.x, extents.y, -extents.z),
            center + new Vector3(extents.x, extents.y, -extents.z),
            center + new Vector3(extents.x, extents.y, extents.z),
            center + new Vector3(-extents.x, extents.y, extents.z)
        };
    }

    void OnDestroy()
    {
        if (lineRenderers != null)
        {
            foreach (var lineRenderer in lineRenderers)
            {
                if (lineRenderer != null)
                {
                    Destroy(lineRenderer.gameObject);
                }
            }
        }
    }
}