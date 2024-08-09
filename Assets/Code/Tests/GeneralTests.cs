using EasyButtons;
using UnityEngine;

[ExecuteAlways]
public class GeneralTests : MonoBehaviour
{
    public GameObject contentPrefab;
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
    
    [Button]
    private void SpawnContent()
    {
        MeshFilter meshFilter = contentPrefab.GetComponentInChildren<MeshFilter>();

        float localMeshHeight = meshFilter.sharedMesh.bounds.size.y;
        float worldMeshHeight = localMeshHeight * contentPrefab.transform.lossyScale.y * 0.5f;
        
        Instantiate(contentPrefab, transform.position + transform.up * worldMeshHeight, contentPrefab.transform.rotation, transform);
    }
}
