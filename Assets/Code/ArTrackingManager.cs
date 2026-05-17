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
    private ARTrackedImage _currentTrackedImg;
    private string _currentTrackedName;
    
    private Vector3 _object1Offset = new Vector3(0f, 0.01f, 0.18f);
    private Vector3 _object2Offset = new Vector3(0.005f, -0.01f, 0.28f);
    private Vector3 _object3Offset = new Vector3(-0.015f, 0.005f, 0.215f);
    
    private void OnEnable()
    { 
        trackedImgManager.trackablesChanged.AddListener(OnChanged);
        dataProvider.DoubleTouchEvent.AddListener(RespawnObject);
    }

    private void OnDisable()
    {
        trackedImgManager.trackablesChanged.RemoveListener(OnChanged);
        dataProvider.DoubleTouchEvent.RemoveListener(RespawnObject);
    }

    private void OnChanged(ARTrackablesChangedEventArgs<ARTrackedImage> eventArgs)
    {
        if (eventArgs.added.Count == 0)
            return;

        _currentTrackedImg = eventArgs.added[0];
        _currentTrackedName = _currentTrackedImg.referenceImage.name;

        imageRecognizedEvent.Invoke(_currentTrackedName);

        if (_currentTrackedName == Object1TrackedImgName)
            _currentPrefab = arObject1Prefab;
        else if (_currentTrackedName == Object2TrackedImgName)
            _currentPrefab = arObject2Prefab;
        else if (_currentTrackedName == Object3TrackedImgName)
            _currentPrefab = arObject3Prefab;

        if (spawnedContent)
            DestroyImmediate(spawnedContent);

        Vector3 offsetPos = _currentTrackedImg.transform.position;

        if (_currentTrackedName == Object1TrackedImgName)
            offsetPos += GetWorldOffset(_currentTrackedImg.transform, _object1Offset);
        else if (_currentTrackedName == Object2TrackedImgName)
            offsetPos += GetWorldOffset(_currentTrackedImg.transform, _object2Offset);
        else if (_currentTrackedName == Object3TrackedImgName)
            offsetPos += GetWorldOffset(_currentTrackedImg.transform, _object3Offset);

        spawnedContent = Instantiate(
            _currentPrefab,
            offsetPos,
            _currentTrackedImg.transform.rotation,
            _currentTrackedImg.transform
        );

        isSpawned = true;

        dataProvider.SetArObjectTransform(spawnedContent.transform, pathVisualizer);
    }

    private void RespawnObject()
    {
        if (_currentTrackedImg == null)
            return;

        if (dataProvider.TryGetParamValue(out var transformData))
        {
            DestroyImmediate(spawnedContent);

            var x = transformData[0] * _currentTrackedImg.transform.right;
            var y = transformData[1] * _currentTrackedImg.transform.up;
            var z = transformData[2] * _currentTrackedImg.transform.forward;

            var offsetPos = _currentTrackedImg.transform.position + x + y + z;

            spawnedContent = Instantiate(
                _currentPrefab,
                offsetPos,
                _currentTrackedImg.transform.rotation,
                _currentTrackedImg.transform
            );

            if (transformData[3] != 0)
            {
                float scale = transformData[3];
                spawnedContent.transform.localScale = Vector3.one * scale;
            }

            if (transformData[4] != 0)
            {
                float yRotation = transformData[4];
                spawnedContent.transform.localRotation = Quaternion.Euler(0, yRotation, 0);
            }

            dataProvider.SetArObjectTransform(spawnedContent.transform, pathVisualizer);
        }
    }

    private Vector3 GetWorldOffset(Transform trackedTransform, Vector3 localOffset)
    {
        return trackedTransform.right * localOffset.x +
               trackedTransform.up * localOffset.y +
               trackedTransform.forward * localOffset.z;
    }

    private void Update()
    {
        if (_currentTrackedImg)
        {
            imagePosRotTxt.text =
                $"img pos: {_currentTrackedImg.transform.position}" +
                $"\nimg rot: {_currentTrackedImg.transform.rotation.eulerAngles}";
        }
    }
}