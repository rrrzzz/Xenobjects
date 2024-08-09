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
    
    private void OnEnable()
    { 
        trackedImgManager.trackedImagesChanged += OnChanged;
        dataProvider.SingleTouchEvent.AddListener(RespawnObject);
    }

    private void RespawnObject()
    {
        if (dataProvider.TryGetParamValue(out var offset))
        {
            DestroyImmediate(spawnedContent);
            // imagePosRotTxt.text = "img pos: " +  _trackedImg.transform.position +"\nimg rot:" +  
            //                       _trackedImg.transform.rotation + "\noffset: " + offset;

            var x = offset.x * _trackedImg.transform.right;
            var y = offset.y * _trackedImg.transform.up;
            var z = offset.z * _trackedImg.transform.forward;
            
            var offsetPos = _trackedImg.transform.position + x + y + z;
            spawnedContent.transform.position = offsetPos;
            // spawnedContent = Instantiate(contentPrefab, offsetPos, contentPrefab.transform.rotation, _trackedImg.transform);
            dataProvider.SetArObject2Transform(spawnedContent.transform);
        }
    }

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
 
            MeshFilter meshFilter = contentPrefab.GetComponentInChildren<MeshFilter>();

            float localMeshHeight = meshFilter.sharedMesh.bounds.size.y;
            float worldMeshHeight = localMeshHeight * contentPrefab.transform.lossyScale.y * 0.5f;
            // Debug.Log("Mesh world-space height: " + worldMeshHeight);
            
            var offsetPos = _trackedImg.transform.position + _trackedImg.transform.up * .2f + _trackedImg.transform.forward * 0.2f;

            spawnedContent = Instantiate(contentPrefab, offsetPos, contentPrefab.transform.rotation, _trackedImg.transform);
            isSpawned = true;
            imagePosRotTxt.text = "Object spawned! Mesh world-space height: " + worldMeshHeight;


            // dataProvider.SetArObject2Transform(spawnedContent.transform);
        }

        if (eventArgs.removed.Count != 0)
        {
            isSpawned = false;
            dataProvider.arObjectTr = null;
        }

        // if (eventArgs.updated.Count == 0) return;
        // Quaternion additionalRotation = Quaternion.Euler(0f, 180f, 0f);
        //
        // spawnedContent.transform.position = _trackedImg.transform.position + Vector3.forward * 0.4f;
        // spawnedContent.transform.rotation = _trackedImg.transform.rotation * additionalRotation;
    }

    private void Update()
    {
        if (_trackedImg)
        {
            imagePosRotTxt.text = $"img pos: {_trackedImg.transform.position}" +
                                  $"\nimg rot: {_trackedImg.transform.rotation.eulerAngles}, " +
                                  $"up: {_trackedImg.transform.up}";
            if (isSpawned)
            {
                imagePosRotTxt.text += $"\nspawned rot: {spawnedContent.transform.rotation.eulerAngles}, " +
                                       $"local rot: {spawnedContent.transform.localRotation.eulerAngles}";
            }
        }
    }
}
