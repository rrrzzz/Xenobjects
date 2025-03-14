﻿using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(PSMeshRendererUpdater))]
public class MeshRendererEditor : UnityEditor.Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        var myScript = (PSMeshRendererUpdater)target;
        if (GUILayout.Button("Update Mesh Renderer"))
        {
            myScript.UpdateMeshEffect();
        }
    }
}