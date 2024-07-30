using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR.ARFoundation;


public class RadialSpawner : MonoBehaviour
{
    public ARTrackedImageManager trackedImgManager;
    
    private void OnEnable() => trackedImgManager.trackedImagesChanged += OnChanged;

    private void OnDisable() => trackedImgManager.trackedImagesChanged -= OnChanged;
    
    private void OnChanged(ARTrackedImagesChangedEventArgs eventArgs)
    {
        _centerPos = eventArgs.added[0].transform.position;
        StartCoroutine(ExplodeAfterDelay());
    }

    private IEnumerator ExplodeAfterDelay()
    {
        yield return new WaitForSeconds(3);
        _isStarted = true;
    }

    private Transform _centerTr;
    public float height;
    public GameObject[] objsToSpawn;
    public GameObject lastObj;
    public float radius;
    public float degreeStep;
    public float spawnInterval;
    
    public UnityEvent explosionEvent;

    private Vector3 _centerPos;
    private float _currentDegrees;
    private float _spawnTime;
    private GameObject _currentObjToSpawn;
    private int _currentObjIdx;
    private Queue<GameObject> _spawnedObjects = new Queue<GameObject>();
    private bool _isStarted;

    void Start()
    {
        _spawnTime = spawnInterval;
        _currentObjToSpawn = objsToSpawn[_currentObjIdx];
    }

    private void Update()
    {
        if (!_isStarted)
        {
            return;
        }
        
        if (_currentObjIdx == objsToSpawn.Length)
            return;
    
        if (_currentDegrees > 360)
        {
            _currentObjIdx++;
            _currentDegrees = 0;
        
            if (_currentObjIdx == objsToSpawn.Length)
            {
                SpawnLastImmediate();
                return;
            }
        
            _currentObjToSpawn = objsToSpawn[_currentObjIdx];
        }
    
        if (Time.realtimeSinceStartup - _spawnTime > spawnInterval)
        {
            _spawnTime = Time.realtimeSinceStartup;
            var degreesInRad = Mathf.Deg2Rad * _currentDegrees;
            var spawnPos = new Vector3(Mathf.Cos(degreesInRad) * radius + _centerPos.x, _centerPos.y + height, 
                Mathf.Sin(degreesInRad)  * radius + _centerPos.z);
            _spawnedObjects.Enqueue(Instantiate(_currentObjToSpawn, spawnPos, Quaternion.identity));
            if (_currentObjIdx > 0)
            {
                Destroy(_spawnedObjects.Dequeue());
            }
            _currentDegrees += degreeStep;
        }        
    }

    private void SpawnLastImmediate()
    {
        _centerPos.y = height;
        Instantiate(lastObj, _centerPos, Quaternion.identity);
        while (_currentDegrees < 360)
        {
            var degreesInRad = Mathf.Deg2Rad * _currentDegrees;
            radius *= .5f;
            var spawnPos = new Vector3(Mathf.Cos(degreesInRad) * radius + _centerPos.x, height, 
                Mathf.Sin(degreesInRad) * radius + _centerPos.z);
            Instantiate(lastObj, spawnPos, Quaternion.identity);
            _currentDegrees += degreeStep;
        }
        
        explosionEvent.Invoke();
    }
}
