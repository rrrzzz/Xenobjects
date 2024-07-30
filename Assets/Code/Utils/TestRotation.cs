using EasyButtons;
using UnityEngine;

[ExecuteAlways]
public class TestRotation : MonoBehaviour
{
    public float rotX;
    public float rotY;

    [Button]
    private void RotateAroundY()
    {
        var center = GetComponent<MeshFilter>().sharedMesh.bounds.center;
        center = transform.TransformPoint(center);
        transform.RotateAround(center, Vector3.up, rotY);
    }
    
    [Button]
    private void RotateAroundX()
    {
        var center = GetComponent<MeshFilter>().sharedMesh.bounds.center; 
        center = transform.TransformPoint(center);
        transform.RotateAround(center, Vector3.right, rotX);
    }
}
