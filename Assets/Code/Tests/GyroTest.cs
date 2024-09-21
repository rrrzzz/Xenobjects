using System.Globalization;
using TMPro;
using UnityEngine;

public class GyroTest : MonoBehaviour
{
    public float threshold = 2;
    public TMP_Text anchorRotationText;
    void Start()
    {
        Input.gyro.enabled = true;
    }
        
    void Update()
    {
        Quaternion rotation = Input.gyro.attitude;
        // anchorRotationText.text = rotation.ToString();
        Vector3 angles = rotation.eulerAngles;
        //    
        anchorRotationText.text = angles.ToString();
    
        // Check if x and z rotations are close to 0 or 360
        if ((angles.x < threshold || angles.x > 360 - threshold) &&
            (angles.z < threshold || angles.z > 360 - threshold))
        {
            Debug.Log("Phone is horizontal");
        }
    }
}
