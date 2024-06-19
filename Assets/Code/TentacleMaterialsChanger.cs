using System.Linq;
using UnityEngine;

[ExecuteAlways]
public class TentacleMaterialsChanger : MonoBehaviour
{
    public bool setClippingValues;
    public float coordinate;
    public float[] clippingValues;
    public Material[] materials;

    private static readonly int ClipEnd = Shader.PropertyToID("_ClipEnd");
    public bool reassignMaterials;
    public bool isRealTime;
    private static readonly int ClipCoordinate = Shader.PropertyToID("_ClipCoordinate");

    // Update is called once per frame
    void Update()
    {
        
    }
}
