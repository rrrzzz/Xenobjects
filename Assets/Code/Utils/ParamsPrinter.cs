using System.Collections;
using System.Collections.Generic;
using EasyButtons;
using UnityEngine;

[ExecuteAlways]
public class ParamsPrinter : MonoBehaviour
{
    
    [Button]
    public void PrintLocalPos()
    {
        print(FormatVector3(transform.localPosition));
    }
    
    [Button]
    public void PrintColor()
    {
        var mat = GetComponent<MeshRenderer>().sharedMaterial;
        print(FormatColor(mat.GetColor("_TintColor")));
    }
    
    [Button]
    public void PrintColorPs()
    {
        var mat = GetComponent<ParticleSystem>().GetComponent<Renderer>().sharedMaterial;
        print(FormatColor(mat.GetColor("_TintColor")));
    }
    
    private string FormatVector3(Vector3 vec3)
    {
        // Format each component to the desired format with 'f' suffix and specified precision
        string x = vec3.x.ToString("0.000") + "f";
        string y = vec3.y.ToString("0.000") + "f";
        string z = vec3.z.ToString("0.000") + "f";

        // Combine the formatted components into the final string
        return $"({x}, {y}, {z})";
    }
    
    private string FormatColor(Color color)
    {
        // Format each component to the desired format with 'f' suffix and specified precision
        string r = color.r.ToString("0.00000") + "f";
        string g = color.g.ToString("0.00000") + "f";
        string b = color.b.ToString("0.00000") + "f";
        string a = color.a.ToString("0.00000") + "f";

        // Combine the formatted components into the final string
        return $"({r}, {g}, {b}, {a})";
    }
    
    // Update is called once per frame
    void Update()
    {
        
    }
}
