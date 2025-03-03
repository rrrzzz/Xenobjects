using Code;
using UnityEngine;

public class ArCeoMinimal : MonoBehaviour
{
    public MovementInteractionTestProvider dataProvider;
    public GameObject chosenPrefab;
    public GameObject[] prefabs;
    public Transform[] objectPositions;
    private GameObject _currentPrefab;

    
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.F))
        {
            SpawnPrefab(prefabs[0]);
        }
        
        if (Input.GetKeyDown(KeyCode.G))
        {
            SpawnPrefab(prefabs[1]);
        }
        
        if (Input.GetKeyDown(KeyCode.H))
        {
            SpawnPrefab(prefabs[2]); 
        }
    }

    void SpawnPrefab(GameObject prefab)
    {
        
    }
}
