using Code;
using UnityEngine;

public class ArCeoMinimal : MonoBehaviour
{
    public MovementInteractionTestProvider dataProvider;
    public GameObject[] prefabs;
    public Transform[] objectPositions;
    private GameObject _currentPrefab;
    
    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.F))
        {
            SpawnPrefab(prefabs[0], objectPositions[0]);
        }
        
        if (Input.GetKeyDown(KeyCode.G))
        {
            SpawnPrefab(prefabs[1], objectPositions[1]);
        }
        
        if (Input.GetKeyDown(KeyCode.H))
        {
            SpawnPrefab(prefabs[2], objectPositions[2]); 
        }
    }

    void SpawnPrefab(GameObject prefab, Transform t)
    {
        Destroy(_currentPrefab);
        _currentPrefab = Instantiate(prefab);
        _currentPrefab.SetActive(true);
    }
}
