using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShaderTest : MonoBehaviour
{
    public Renderer rend;
    private static readonly int Distortion = Shader.PropertyToID("_Distortion");

    // Start is called before the first frame update
    void Start()
    {


    }

    // Update is called once per frame
    void Update()
    {
        rend.material.SetFloat(Distortion, Mathf.Sin(Time.time) * 100) ;        
    }
}
