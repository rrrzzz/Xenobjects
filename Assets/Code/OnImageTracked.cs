using System.Collections;
using UnityEngine;
using UnityEngine.XR.ARFoundation;

public class OnImageTracked : MonoBehaviour
{
    public ARTrackedImageManager trackedImgManager;
    public GameObject spawnAndExplode;
    
    private void OnEnable() => trackedImgManager.trackedImagesChanged += OnChanged;

    private void OnDisable() => trackedImgManager.trackedImagesChanged -= OnChanged;
    
    private void OnChanged(ARTrackedImagesChangedEventArgs eventArgs)
    {
        StartCoroutine(ExplodeAfterDelay());
    }

    private IEnumerator ExplodeAfterDelay()
    {
        yield return new WaitForSeconds(3);
        spawnAndExplode.SetActive(true);
    }
}
