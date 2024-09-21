using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ToggleTest : MonoBehaviour
{
    public Toggle toggle;

    // Update is called once per frame
    void Update()
    {
        Debug.Log(toggle.isOn);
    }
}
