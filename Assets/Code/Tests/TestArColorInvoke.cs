using System.Globalization;
using Code;
using TMPro;
using UnityEngine; 
using UnityEngine.UI;

public class TestArColorInvoke : MonoBehaviour
{
    public ArCeo arCeo;
    public Button button;
    public ARColorAnalyzer colorAnalyzer;

    public bool isAutoModeOn;

    public float interval = 3f;
    public TMP_Text debugText;
    
    private float _prevTime;
    private Vector3 _verticalLineRotation = Vector3.zero;
    private Vector3 _horizontalLineRotation = Vector3.zero;
    private readonly Color _defCrosshairColor = Color.white;
    private readonly Color _targetCapturedColor = Color.red;
    private bool _isManualMode = true;
    private bool _isPhoneHorizontal;
    private float _crossCheckInterval = 0.5f;
    private float _crossCheckStartTime;
    
    void Start()
    {
        button.onClick.AddListener(() =>
        {
            _isManualMode = !_isManualMode;
        });
        
        colorAnalyzer.dominantColor = Color.blue;
        arCeo.crosshair.SetActive(true);
    }

    // Update is called once per frame
    void Update()
    {
        if (!CheckPhoneAlignedWithCrosshair())
            return;

        if (_isManualMode)
        {
            if (Input.touchCount == 1)
            {
                var touch = Input.GetTouch(0);
                if (touch.phase != TouchPhase.Began) 
                    return;
                
                colorAnalyzer.TryDetermineCenteredAtCross();
            }

            return;
        }
        
        if (Time.realtimeSinceStartup - _crossCheckStartTime < _crossCheckInterval) 
            return;

        _crossCheckStartTime = Time.realtimeSinceStartup;
        if (colorAnalyzer.isProcessingImage) 
            return;
        
        if (arCeo.TryGetParamValue(arCeo.inputFloats, out var val))
        {
            colorAnalyzer.crossHorVertErrorMargin = val[1] == 0 ? colorAnalyzer.crossHorVertErrorMargin : val[1];
        }
            
        colorAnalyzer.TryDetermineCenteredAtCross();

        // if (CheckPhoneAlignedWithCrosshair() && Input.touchCount == 1)
        // {
        //     
        //     var touch = Input.GetTouch(0);
        //     if (touch.phase != TouchPhase.Began) 
        //         return;
        //     
        //     if (arCeo.TryGetParamValue(arCeo.inputFloats, out var val))
        //     {
        //         colorAnalyzer.crossHorVertErrorMargin = val[1] == 0 ? colorAnalyzer.crossHorVertErrorMargin : val[1];
        //     }
        //     
        //     colorAnalyzer.TryDetermineCenteredAtCross();
        // }
    }
    
    private bool CheckPhoneApproximatelyHorizontal()
    {
        Quaternion rotation = Input.gyro.attitude;
        Vector2 angles = NormalizeRotationAngles(rotation.eulerAngles);
               
        arCeo.anchorRotationText.text = angles.ToString();

        var xError = Mathf.Abs(angles.x);
        var yError = Mathf.Abs(angles.y);

        var currentThreshold = arCeo.horizontalThreshold;

        var isXHorizontal = xError < currentThreshold;
        var isYHorizontal = yError < currentThreshold;
            
        return isXHorizontal && isYHorizontal;
    }
    
    private bool CheckPhoneAlignedWithCrosshair()
    {
        Quaternion rotation = Input.gyro.attitude;
        Vector2 angles = NormalizeRotationAngles(rotation.eulerAngles);
        arCeo.anchorRotationText.text = angles.ToString();

        var xError = Mathf.Abs(angles.x);
        var yError = Mathf.Abs(angles.y);

        var currentAimingThreshold = arCeo.crosshairAimingThreshold;
            
        var isXCaptured = xError < currentAimingThreshold;
        var isYCaptured = yError < currentAimingThreshold;
            
        _horizontalLineRotation.z = isXCaptured ? 0: -angles.x; 
        _verticalLineRotation.z = isYCaptured ? 0: -angles.y;

        arCeo.horizontalLineImage.color = isXCaptured ? _targetCapturedColor : _defCrosshairColor;
        arCeo.verticalLineImage.color = isYCaptured ? _targetCapturedColor : _defCrosshairColor;

        arCeo.crosshairHorizontalLine.rotation = Quaternion.Euler(_horizontalLineRotation);
        arCeo.crosshairVerticalLine.rotation = Quaternion.Euler(_verticalLineRotation);
            
        return isXCaptured && isYCaptured;
    }
    
    private Vector3 NormalizeRotationAngles(Vector3 rotation)
    {
        if (rotation.x > 180)
            rotation.x -= 360;

        if (rotation.y > 180)
            rotation.y -= 360;
            
        if (rotation.z > 180)
            rotation.z -= 360;
            
        return rotation * -1;
    }
}
