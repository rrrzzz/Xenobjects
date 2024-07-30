using EasyButtons;
using UnityEditor;
using UnityEngine;


[ExecuteAlways]
public class MeshSaver : MonoBehaviour
{
    [Button]
    private void SaveMeshes()
    {
        var meshFilters = GetComponentsInChildren<MeshFilter>();
        foreach (var mf in meshFilters)
        {
            var savedMeshFolder = "Assets/GeneratedMeshes/";
            var path = savedMeshFolder + mf.name + ".asset";
            
            MeshUtility.Optimize(mf.sharedMesh);
        
            AssetDatabase.CreateAsset(mf.sharedMesh, path);
            AssetDatabase.SaveAssets();
        }
    }
}
