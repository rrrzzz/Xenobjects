using System;
using EasyButtons;
using UnityEngine;

public class ShaderTest : MonoBehaviour
{
    //_GradientStrength - orb
    //_TintColor - ink sink above, orb 
    //_Color - Steam, 
    public Renderer rend;
    public ShaderProps shaderPropertyName;
    public int materialIdx;
    public float floatToSet;
    public Color colorToSet;
    [ColorUsage(true, true)]
    public Color hdrToSet;
    public float originalIntensity = 1.626481f;
    
    private Material _material;
    private Color _savedColor;

    public void OnValidate()
    {
        if (!Application.isPlaying)
        {
            return;
        }
        SetHdrColor();
        SetFloat();
    }

    [Button]
    public void SetColor()
    {
        SetMaterial();
        _material.SetColor(shaderPropertyName.ToString(), colorToSet);
    }
    
    [Button]
    public void SetHdrColor()
    {
        SetMaterial();
        _material.SetColor("_TintColor", hdrToSet);
    }

    [Button]
    public void SetFloat()
    {
        SetMaterial();
        _material.SetFloat(shaderPropertyName.ToString(), floatToSet);
    }

    private void SetMaterial()
    {
        if (rend)
        {
            _material = rend.materials[materialIdx];
        }
    }
}

public enum ShaderProps
{
    _Color,
    _TintColor,
    _GradientStrength
}
