using Code;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR.ARFoundation;

public class ArTrackingManager : MonoBehaviour
{
    public UnityEvent<string> imageRecognizedEvent; 
    public TMP_Text imagePosRotTxt;
    public ARTrackedImageManager trackedImgManager;
    public GameObject contentPrefab;
    // public GameObject[] contentPrefabs;
    public bool isSpawned;
    public GameObject spawnedContent;
    public ARMovementInteractionDataProvider dataProvider;
    
    private ARTrackedImage _trackedImg;
    
    private void OnEnable() => trackedImgManager.trackedImagesChanged += OnChanged;

    private void OnDisable() => trackedImgManager.trackedImagesChanged -= OnChanged;
    
    private void OnChanged(ARTrackedImagesChangedEventArgs eventArgs)
    {
        if (eventArgs.added.Count != 0)
        {
            _trackedImg = eventArgs.added[0];
            imageRecognizedEvent.Invoke(_trackedImg.name);
            
            // // TODO: Check if this is necessary
            // if (spawnedContent)
            // {
            //     Destroy(spawnedContent);
            // }
            
            spawnedContent = Instantiate(contentPrefab, _trackedImg.transform);
            isSpawned = true;

            dataProvider.SetArObject2Transform(spawnedContent.transform);
        }

        if (eventArgs.removed.Count != 0)
        {
            isSpawned = false;
            dataProvider.arObjectTr = null;
        }

        if (eventArgs.updated.Count == 0) return;
        Quaternion additionalRotation = Quaternion.Euler(0f, 180f, 0f);
        spawnedContent.transform.position = _trackedImg.transform.position;
        spawnedContent.transform.rotation = _trackedImg.transform.rotation * additionalRotation;
    }

    private void Update()
    {
        if (_trackedImg)
        {
            imagePosRotTxt.text = "img pos: " +  _trackedImg.transform.position +"\nimg rot:" +  
                               _trackedImg.transform.rotation;
        }
    }
}
