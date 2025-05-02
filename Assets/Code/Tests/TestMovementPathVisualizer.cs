using System.Collections.Generic;
using System.IO;
using EasyButtons;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(LineRenderer))]
public class TestMovementPathVisualizer : MonoBehaviour
{
    public float recordInterval = 0.2f;
    public Button drawPathBtn;
    public Button resetPathBtn;
    private LineRenderer _lineRenderer;
    private readonly List<Vector3> _recordedPositions = new List<Vector3>();
    private float _timer;
    private Transform _camTransform;
    private string _filePath;
    
    void Start()
    {
        if (drawPathBtn)
        {
            drawPathBtn.onClick.AddListener(UpdateLine);
        }
        
        if (resetPathBtn)
        {
            resetPathBtn.onClick.AddListener(RestartTracking);
        }
        
        _filePath = Path.Combine(Application.persistentDataPath, "positions.bin");
        _lineRenderer = GetComponent<LineRenderer>();
        _camTransform = Camera.main.transform;
    }

    [Button]
    private void RestartTracking()
    {
        _recordedPositions.Clear();
        _lineRenderer.positionCount = 0;
        _timer = 0;
    }
    
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            UpdateLine();
        }
        
        _timer += Time.deltaTime;
        if (_timer >= recordInterval)
        {
            Vector3 currentPos = _camTransform.position;
            _recordedPositions.Add(currentPos);
            _timer = 0f;
        }
    }

    [Button]
    void UpdateLine()
    {
        // Efficient line updating involves minimal overhead:
        // Just adjust the line vertex count and set the positions.
        _lineRenderer.positionCount = _recordedPositions.Count;
        for (int i = 0; i < _recordedPositions.Count; i++)
        {
            _lineRenderer.SetPosition(i, _recordedPositions[i]);
        }
    }
    
    [Button]
    public void SavePositions()
    {
        using (BinaryWriter writer = new BinaryWriter(File.Open(_filePath, FileMode.Create)))
        {
            writer.Write(_recordedPositions.Count);
            foreach (Vector3 pos in _recordedPositions)
            {
                writer.Write(pos.x);
                writer.Write(pos.y);
                writer.Write(pos.z);
            }
        }
    }

    [Button]
    public void LoadPositions()
    {
        using (BinaryReader reader = new BinaryReader(File.Open(_filePath, FileMode.Open)))
        {
            int count = reader.ReadInt32();
            _recordedPositions.Clear();
            for (int i = 0; i < count; i++)
            {
                float x = reader.ReadSingle();
                float y = reader.ReadSingle();
                float z = reader.ReadSingle();
                _recordedPositions.Add(new Vector3(x, y, z));
            }
        }
    }
}
