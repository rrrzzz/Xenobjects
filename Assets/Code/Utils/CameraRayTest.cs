using System.Collections;
using System.Collections.Generic;
using EasyButtons;
using UnityEngine;

public class CameraRayTest : MonoBehaviour
{
    public Transform target;
    // Start is called before the first frame update
    void Start()
    {
        
    }
    
    [Button]
    private void RayTest()
    {
        var camTr = Camera.main.transform;
        Ray ray = new Ray(camTr.position, camTr.forward);
        
        Debug.DrawRay(ray.origin, ray.direction * 10, Color.red);
        if (Physics.Raycast(camTr.position, camTr.forward, out var hit))
        {
            target.position = hit.point;
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
