using System.Collections;
using System.Collections.Generic;
using SplineMesh;
using UnityEngine;

public class Test : MonoBehaviour
{
    private Spline spline;

    public bool scaleToZero;
    // Start is called before the first frame update
    void Start()
    {
        spline = GetComponentInParent<Spline>();
    }

    // Update is called once per frame
    void Update()
    {
        if (scaleToZero)
        {
            for (int i = 0; i < spline.nodes.Count; i++)
            {

                spline.nodes[i].Scale = Vector2.zero;

            }
            scaleToZero = false;
        }
    }
}
