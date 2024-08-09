using UnityEngine;

public class ARAxisVisualizer : MonoBehaviour
{
    public float sphereRadius = 0.1f;
    public float arrowLength = 0.5f;
    public float arrowWidth = 0.02f;

    private GameObject sphere;
    private GameObject xArrow, yArrow, zArrow;

    void Start()
    {
        CreateSphere();
        CreateArrows();
    }

    void CreateSphere()
    {
        sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        sphere.transform.SetParent(transform);
        sphere.transform.localPosition = Vector3.zero;
        sphere.transform.localScale = Vector3.one * sphereRadius * 2;
    }

    void CreateArrows()
    {
        xArrow = CreateArrow(Color.red, Vector3.right);
        yArrow = CreateArrow(Color.green, Vector3.up);
        zArrow = CreateArrow(Color.blue, Vector3.forward);
    }

    GameObject CreateArrow(Color color, Vector3 direction)
    {
        GameObject arrow = new GameObject($"Arrow_{direction.ToString()}");
        arrow.transform.SetParent(transform);

        GameObject line = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        line.transform.SetParent(arrow.transform);
        line.transform.localScale = new Vector3(arrowWidth, arrowLength / 2, arrowWidth);
        line.transform.localPosition = direction * (arrowLength / 2);
        line.transform.localRotation = Quaternion.FromToRotation(Vector3.up, direction);

        GameObject cone = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        cone.transform.SetParent(arrow.transform);
        cone.transform.localScale = new Vector3(arrowWidth * 3, arrowWidth * 3, arrowWidth * 3);
        cone.transform.localPosition = direction * arrowLength;
        cone.transform.localRotation = Quaternion.FromToRotation(Vector3.up, direction);

        Renderer[] renderers = arrow.GetComponentsInChildren<Renderer>();
        foreach (var renderer in renderers)
        {
            renderer.material.color = color;
        }

        return arrow;
    }

    public void SetPosition(Vector3 position)
    {
        transform.position = position;
    }

    public void SetRotation(Quaternion rotation)
    {
        transform.rotation = rotation;
    }
}