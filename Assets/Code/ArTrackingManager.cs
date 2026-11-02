using System.Collections.Generic;
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

    private readonly Vector3 _object1Offset = new Vector3(0f, 0.01f, 0.18f);
    private readonly Vector3 _object2Offset = new Vector3(0.005f, -0.01f, 0.28f);
    private readonly Vector3 _object3Offset = new Vector3(-0.015f, 0.005f, 0.215f);

    private Dictionary<string, (GameObject prefab, Vector3 offset)> _markerContent;

    private void Awake()
    {
        _markerContent = new Dictionary<string, (GameObject prefab, Vector3 offset)>
        {
            { Object1TrackedImgName, (arObject1Prefab, _object1Offset) },
            { Object2TrackedImgName, (arObject2Prefab, _object2Offset) },
            { Object3TrackedImgName, (arObject3Prefab, _object3Offset) }
        };
    }

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

        if (!_markerContent.TryGetValue(_currentTrackedName, out var content) || !content.prefab)
        {
            Debug.LogWarning($"No prefab mapped to tracked image '{_currentTrackedName}', nothing spawned.");
            return;
        }

        _currentPrefab = content.prefab;

        if (spawnedContent)
            Destroy(spawnedContent);

        Vector3 offsetPos = _currentTrackedImg.transform.position +
                            GetWorldOffset(_currentTrackedImg.transform, content.offset);

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
        if (_currentTrackedImg == null || !_currentPrefab)
            return;

        if (dataProvider.TryGetParamValue(out var transformData))
        {
            if (spawnedContent)
                Destroy(spawnedContent);

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
        if (_currentTrackedImg && imagePosRotTxt)
        {
            imagePosRotTxt.text =
                $"img pos: {_currentTrackedImg.transform.position}" +
                $"\nimg rot: {_currentTrackedImg.transform.rotation.eulerAngles}";
        }
    }
}