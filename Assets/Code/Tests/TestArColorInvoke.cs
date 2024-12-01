using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TestArColorInvoke : MonoBehaviour
{
    public Button button;
    public ARColorAnalyzer colorAnalyzer;

    public bool isAutoModeOn;

    public float interval = 3f;
    public TMP_Text debugText;
    
    private float _prevTime;

    void Start()
    {
        button.onClick.AddListener(() =>
        {
            _prevTime = Time.time;
            isAutoModeOn = !isAutoModeOn;
        });
    }

    // Update is called once per frame
    void Update()
    {
        if (isAutoModeOn)
        {
            if (Time.time - _prevTime > interval)
            {
                debugText.text = $"Capturing image at: {Time.time}";
                colorAnalyzer.TryCaptureImage();
                _prevTime = Time.time;
            }

            return;
        }

        debugText.text = "Auto mode is off";
        
        if (Input.touchCount == 1 && !isAutoModeOn)
        {
            var touch = Input.GetTouch(0);
            if (touch.phase != TouchPhase.Began) 
                return;
                
            colorAnalyzer.TryCaptureImage();
        }
    }
}
