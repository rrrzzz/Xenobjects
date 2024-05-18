using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;


public class RadialSpawner : MonoBehaviour
{
    public Transform centerTr;
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

    void Start()
    {
        _centerPos = centerTr.position;
        _spawnTime = spawnInterval;
        _currentObjToSpawn = objsToSpawn[_currentObjIdx];
    }

    private void Update()
    {
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
            var spawnPos = new Vector3(Mathf.Cos(degreesInRad) * radius + _centerPos.x, height, 
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
