using Code;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR.ARFoundation;

public class ArTrackingManager : MonoBehaviour
{
    private const string Object1TrackedImgName = "Object1Marker";
    private const string Object2TrackedImgName = "Object2Marker";
    private const string Object3TrackedImgName = "Object3Marker";
     
    public UnityEvent<string> imageRecognizedEvent; 
    public TMP_Text imagePosRotTxt;
    public ARTrackedImageManager trackedImgManager;
    public GameObject arObject1Prefab;
    public GameObject arObject2Prefab;
    public GameObject arObject3Prefab;
    public MovementPathVisualizer pathVisualizer;

    public bool isSpawned;
    public GameObject spawnedContent;
    public ARMovementInteractionDataProvider dataProvider;
    
    private GameObject _currentPrefab;
    private ARTrackedImage _trackedImg1;
    private ARTrackedImage _trackedImg2;
    private ARTrackedImage _trackedImg3;
    private ARTrackedImage _currentTrackedImg;
    private string _currentTrackedName;
    
    private Vector3 _object1Offset = new Vector3(0f, 0.01f, 0.18f);
    private Vector3 _object2Offset = new Vector3(0.005f, -0.01f, 0.28f);
    private Vector3 _object3Offset = new Vector3(-0.015f, 0.005f, 0.215f);
    
    private void OnEnable()
    { 
        trackedImgManager.trackedImagesChanged += OnChanged;
        dataProvider.DoubleTouchEvent.AddListener(RespawnObject);
    }

    private void RespawnObject()
    {
        if (dataProvider.TryGetParamValue(out var transformData))
        {
            // imagePosRotTxt.text = "img pos: " +  _trackedImg.transform.position +"\nimg rot:" +  
            //                       _trackedImg.transform.rotation + "\noffset: " + offset;
            
            DestroyImmediate(spawnedContent);
            var x = transformData[0] * _currentTrackedImg.transform.right;
            var y = transformData[1] * _currentTrackedImg.transform.up;
            var z = transformData[2] * _currentTrackedImg.transform.forward;
            
            var offsetPos = _currentTrackedImg.transform.position + x + y + z;
            spawnedContent = Instantiate(_currentPrefab, offsetPos, _currentTrackedImg.transform.rotation, _currentTrackedImg.transform);
            
            if (transformData[3] != 0)
            {
                var w = transformData[3];
                spawnedContent.transform.localScale = new Vector3(w, w, w);
            }
            
            if (transformData[4] != 0)
            {
                var q = transformData[4];
                spawnedContent.transform.localRotation = Quaternion.Euler(0, q, 0);
            }
            
            dataProvider.SetArObjectTransform(spawnedContent.transform, pathVisualizer);
        }
    }

    private void OnDisable() => trackedImgManager.trackedImagesChanged -= OnChanged;
    
    private void OnChanged(ARTrackedImagesChangedEventArgs eventArgs)
    {
        //TODO fix object spawning only once
        if (eventArgs.added.Count != 0)
        {
            _currentTrackedImg = eventArgs.added[0];
            _currentTrackedName = _currentTrackedImg.referenceImage.name;
            imageRecognizedEvent.Invoke(_currentTrackedName);
            
            if (_currentTrackedImg.referenceImage.name == Object1TrackedImgName)
            {
                //TODO CHANGE THIS TO ONE
                _currentPrefab = arObject1Prefab;
            }
            else if (_currentTrackedImg.referenceImage.name == Object2TrackedImgName)
            {
                _currentPrefab = arObject2Prefab;
            }
            else if (_currentTrackedImg.referenceImage.name == Object3TrackedImgName)
            {
                _currentPrefab = arObject3Prefab;
            }

            if (spawnedContent)
            {
                DestroyImmediate(spawnedContent);
            }

            var offsetPos = _currentTrackedImg.transform.position; 
            
            //TODO: Add three objects
            if (_currentTrackedImg.referenceImage.name == Object1TrackedImgName)
            {
                offsetPos += _currentTrackedImg.transform.right * _object1Offset.x + 
                             _currentTrackedImg.transform.up * _object1Offset.y + 
                             _currentTrackedImg.transform.forward * _object1Offset.z;
            }
            else if (_currentTrackedImg.referenceImage.name == Object2TrackedImgName)
            {
                offsetPos += _currentTrackedImg.transform.right * _object2Offset.x + 
                             _currentTrackedImg.transform.up * _object2Offset.y + 
                             _currentTrackedImg.transform.forward * _object2Offset.z;
            }
            else if (_currentTrackedImg.referenceImage.name == Object3TrackedImgName)
            {
                offsetPos += _currentTrackedImg.transform.right * _object3Offset.x + 
                             _currentTrackedImg.transform.up * _object3Offset.y + 
                             _currentTrackedImg.transform.forward * _object3Offset.z;
            }

            spawnedContent = Instantiate(_currentPrefab, offsetPos, _currentPrefab.transform.rotation, _currentTrackedImg.transform);
            isSpawned = true;

            dataProvider.SetArObjectTransform(spawnedContent.transform, pathVisualizer);
        }
        
        // if (eventArgs.updated.Count != 0)
        // {
        //     var updatedImage = eventArgs.updated[0];
        //     var updatedName = updatedImage.referenceImage.name;
        //     if (updatedName == _currentTrackedName)
        //         return;
        //
        //     _currentTrackedImg = updatedImage;
        //     _currentTrackedName = updatedName;
        //     
        //     imageRecognizedEvent.Invoke(_currentTrackedImg.referenceImage.name);
        //     if (_currentTrackedName == Object1TrackedImgName)
        //     {
        //         //TODO CHANGE THIS TO ONE
        //         _currentPrefab = arObject2Prefab;
        //     }
        //     else if (_currentTrackedName == Object2TrackedImgName)
        //     {
        //         _currentPrefab = arObject2Prefab;
        //     }
        //     else if (_currentTrackedName == Object3TrackedImgName)
        //     {
        //         _currentPrefab = arObject3Prefab;
        //     }
        //
        //     if (spawnedContent)
        //     {
        //         DestroyImmediate(spawnedContent);
        //     }
        //
        //     var offsetPos = _currentTrackedImg.transform.position; 
        //     
        //     //TODO: Add three objects
        //     if (_currentTrackedImg.referenceImage.name == Object2TrackedImgName || _currentTrackedImg.referenceImage.name == Object1TrackedImgName)
        //     {
        //         offsetPos += _currentTrackedImg.transform.right * _object2Offset.x + 
        //                      _currentTrackedImg.transform.up * _object2Offset.y + 
        //                      _currentTrackedImg.transform.forward * _object2Offset.z;
        //     }
        //     else if (_currentTrackedImg.referenceImage.name == Object3TrackedImgName)
        //     {
        //         offsetPos += _currentTrackedImg.transform.right * _object3Offset.x + 
        //                      _currentTrackedImg.transform.up * _object3Offset.y + 
        //                      _currentTrackedImg.transform.forward * _object3Offset.z;
        //     }
        //
        //     spawnedContent = Instantiate(_currentPrefab, offsetPos, _currentPrefab.transform.rotation, _currentTrackedImg.transform);
        //     isSpawned = true;
        //
        //     dataProvider.SetArObjectTransform(spawnedContent.transform);
        // }

        // if (eventArgs.updated.Count == 0) return;
        // Quaternion additionalRotation = Quaternion.Euler(0f, 180f, 0f);
        //
        // spawnedContent.transform.position = _trackedImg.transform.position + Vector3.forward * 0.4f;
        // spawnedContent.transform.rotation = _trackedImg.transform.rotation * additionalRotation;
    }

    private Vector3 GetOffsetPos(Transform trackedImgTransform, Vector3 objectOffset)
    {
         return trackedImgTransform.position += trackedImgTransform.right * objectOffset.x + 
                                     trackedImgTransform.up * objectOffset.y + 
                                     trackedImgTransform.forward * objectOffset.z;
    }

    private void Update()
    {
        if (_currentTrackedImg)
        {
            imagePosRotTxt.text = $"img pos: {_currentTrackedImg.transform.position}" +
                                  $"\nimg rot: {_currentTrackedImg.transform.rotation.eulerAngles}";
        }
    }
}
