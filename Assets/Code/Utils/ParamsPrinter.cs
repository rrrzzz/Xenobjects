using System.Collections;
using System.Collections.Generic;
using EasyButtons;
using UnityEngine;

[ExecuteAlways]
public class ParamsPrinter : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        
    }
    [Button]
    public void PrintColor()
    {
        var mat = GetComponent<MeshRenderer>().sharedMaterial;
        print(FormatColor(mat.GetColor("_TintColor")));
    }
    
    string FormatColor(Color color)
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
