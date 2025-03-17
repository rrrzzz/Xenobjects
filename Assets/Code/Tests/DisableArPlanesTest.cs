using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.ARFoundation;

public class DisableArPlanesTest : MonoBehaviour
{
    public ARPlaneManager arPlaneManager;
    public TMP_Text debugText;
    public Button toggleMeshVisibility;

    private bool _isVisible;

    private void Start()
    {
        toggleMeshVisibility.onClick.AddListener(ToggleArPlanesVisibility);
    }

    // Update is called once per frame
    void Update()
    {
        if (arPlaneManager.trackables.count != 0)
        {
            debugText.text = "Trackable Count: " + arPlaneManager.trackables.count + "\n";
        }
        else
        {
            debugText.text = "No Trackable Found";
        }
    }
    
    private void ToggleArPlanesVisibility()
    {
        _isVisible = !_isVisible;
        foreach (ARPlane plane in arPlaneManager.trackables)
        {
            var meshVisualizer = plane.GetComponent<ARPlaneMeshVisualizer>();
            if (meshVisualizer)
                meshVisualizer.enabled = _isVisible;
        }
    }
}
