using System.Collections;
using System.Collections.Generic;
using Code;
using UnityEngine;

public class ShaderTest : MonoBehaviour
{
    public ARMovementInteractionDataProvider dataProvider;
    
    private readonly int _distortion = Shader.PropertyToID("_Distortion");

    // Start is called before the first frame update
    void Start()
    {
        

    }

    // Update is called once per frame
    // void Update()
    // {
    //     if (dataProvider.arObjectTr)
    //     {
    //         
    //     }
    //     rend.material.SetFloat(_distortion, Mathf.Sin(Time.time) * 100) ;        
    // }
}
