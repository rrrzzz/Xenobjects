using System.Collections.Generic;
using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

namespace Code.CrosshairTests
{
    public class ArCeo : MonoBehaviour
    {
        private const float CheckTimeoutAfterSpawn = 10;
        
        // public Button captureButton;
        public ARAnchorManager anchorManager;
        public ARRaycastManager raycastManager;
        public GameObject crosshair;
        public GameObject planeScanTip;
        public GameObject prefab;
        public GameObject[] prefabs;
        public TMP_InputField inputFloats;
        public float crosshairAimingThreshold = 2;
        public float horizontalThreshold = 10;
        public float horizontalCheckInterval = 1;
        public TMP_Text anchorRotationText;
        public RectTransform verticalLine;
        public RectTransform horizontalLine;
        public Image verticalLineImage;
        public Image horizontalLineImage;
        public ARColorAnalyzer colorAnalyzer;
        [HideInInspector] public bool isObjectSpawned;
        
        private Vector3 _verticalLineRotation = Vector3.zero;
        private Vector3 _horizontalLineRotation = Vector3.zero;
        private bool _planeWasAdded;
        private ARAnchor _currentAnchor;
        private GameObject _currentPrefab;
        private Vector2 _screenCenter;
        private readonly Color _defCrosshairColor = Color.white;
        private readonly Color _targetCapturedColor = Color.red;
        private Transform _phoneTransform;
        private bool _isCapturingImage;
        private bool _isFirstCapture;
        private float _horizontalCheckStartTime;
        private bool _checkingIfSeeingTarget;
        private bool _isTargetFound;
        private List<ARRaycastHit> _planeHits = new List<ARRaycastHit>();
        private int _currentTestingFunctionalityIdx;
        private bool _hasTouchedScreen;

        private void Start()
        {
            // captureButton.onClick.AddListener(() =>
            // {
            //     if (colorAnalyzer.isProcessingImage)
            //     {
            //         return;
            //     }
            //     colorAnalyzer.TryCaptureImage();
            // });
            
            _phoneTransform = Camera.main.transform;
            Input.gyro.enabled = true;
            float centerX = Screen.width / 2f;
            float centerY = Screen.height / 2f;
        
            var lineLength = Mathf.Min(centerX, centerY);
            var lineWidth = verticalLine.rect.width;
            
            horizontalLine.sizeDelta = new Vector2(lineLength, lineWidth);
            verticalLine.sizeDelta = new Vector2(lineWidth, lineLength);
            
            _screenCenter = new Vector2(centerX, centerY);
            _horizontalCheckStartTime = Time.realtimeSinceStartup;
            
        }

        private void Update()
        {
            if (ARSession.state != ARSessionState.SessionTracking)
            {
                anchorRotationText.text = "AR Session not tracking";
                return;
            }
            
            if (colorAnalyzer.isProcessingImage)
            {
                anchorRotationText.text = "Processing image, " + (_checkingIfSeeingTarget ? "checking if seeing cross" : "");
                return;
            }

            if (!_checkingIfSeeingTarget && !_isTargetFound)
            {
                if (Time.realtimeSinceStartup - _horizontalCheckStartTime >= horizontalCheckInterval)
                {
                    anchorRotationText.text = "Checking if phone is horizontal";
                    _horizontalCheckStartTime = Time.realtimeSinceStartup;
                    if (!CheckPhoneApproximatelyHorizontal())
                    {
                        return;
                    }
                    anchorRotationText.text = "Phone is roughly horizontal";

                    // Debug.Log("Phone is roughly horizontal");
                    colorAnalyzer.TryCaptureImage();
                    _checkingIfSeeingTarget = true;
                }
                return;
            }

            if (_checkingIfSeeingTarget && colorAnalyzer.isProcessingSuccess)
            {
                anchorRotationText.text = "Checking if seeing cross, found color " + colorAnalyzer.dominantColor;
                var color = colorAnalyzer.dominantColor;
                if (color != Color.clear)
                {
                    anchorRotationText.text = "Color detected: " + color;
                    colorAnalyzer.isProcessingSuccess = false;
                    _checkingIfSeeingTarget = false;
                    _isTargetFound = true;
                }
                
                colorAnalyzer.isProcessingSuccess = false;
                _checkingIfSeeingTarget = false;
            }
            else if (_checkingIfSeeingTarget && !colorAnalyzer.isProcessingSuccess)
            {
                anchorRotationText.text = "Image processing while _checkingIfSeeingTarget failed";
                _checkingIfSeeingTarget = false;
                return;
            }

            if (_isTargetFound && !_planeWasAdded)
            {
                raycastManager.Raycast(_screenCenter, _planeHits, TrackableType.Planes);
                if (_planeHits.Count != 0)
                {
                    anchorRotationText.text = "Plane found";

                    _planeWasAdded = true;
                    planeScanTip.SetActive(false);
                }
                else
                {
                    anchorRotationText.text = "No plane, showing tip";
                    planeScanTip.SetActive(true);
                }
            }
            
            if (!_planeWasAdded || !_isTargetFound) return;

            if (!crosshair.activeInHierarchy)
            {
                crosshair.SetActive(true);
            }
            
            anchorRotationText.text = "Checking if phone aligned with crosshair";
            if (!CheckPhoneAlignedWithCrosshair())
            {
                return;
            }

            anchorRotationText.text = "Phone aligned, waiting for tap";
            if (Input.touchCount == 1)
            {
                var touch = Input.GetTouch(0);
                if (touch.phase != TouchPhase.Began) 
                    return;
                _hasTouchedScreen = true;
                colorAnalyzer.TryCaptureImage();
                return;
            }
            
            if (!_hasTouchedScreen)
            {
                return;
            }

            if (colorAnalyzer.dominantColor == Color.clear)
            {
                anchorRotationText.text = "No color detected when phone aligned with crosshair, probably moved it away.";
                colorAnalyzer.isProcessingSuccess = false;
                _hasTouchedScreen = false;
                return;
            }
            
            colorAnalyzer.isProcessingSuccess = false;
            
            raycastManager.Raycast(_screenCenter, _planeHits, TrackableType.Planes);
            crosshair.SetActive(false);
            
            if (_planeHits.Count != 0)
            {
                if (colorAnalyzer.dominantColor != Color.clear)
                {
                    SetPrefabBasedOnColor(colorAnalyzer.dominantColor);
                    CreateAnchor(_planeHits[0]);
                }
                
                _horizontalCheckStartTime = Time.realtimeSinceStartup + CheckTimeoutAfterSpawn;
                _isTargetFound = false;
                anchorRotationText.text = $"Waiting for {CheckTimeoutAfterSpawn}s check timeout to expire";
            }
            
            _planeWasAdded = false;
            _hasTouchedScreen = false;
        }

        private void TestFunctionalityById()
        {
            var previousTestingFunctionalityIdx = _currentTestingFunctionalityIdx;
            _currentTestingFunctionalityIdx = TryGetParamValue(inputFloats, out var val) ? (int)val[0] : 0;
            
            switch (_currentTestingFunctionalityIdx)
            {
                case 0:
                    anchorRotationText.text = "No testing functionality selected";
                    return;
                case 1:
                {
                    var isApproxHorizontal = CheckPhoneApproximatelyHorizontal();
                    anchorRotationText.text += "\nPhone is approx horizontal: " + isApproxHorizontal;
                    return;
                }
                case 2:
                {
                    var isAligned = CheckPhoneAlignedWithCrosshair();
                    anchorRotationText.text += "\nPhone is aligned with crosshair: " + isAligned;
                    return;
                }
                case 3:
                {
                    if (colorAnalyzer.isProcessingImage)
                    {
                        if (anchorRotationText.text.Contains("Processing image"))
                        {
                            return;
                        }
                        anchorRotationText.text += " Processing image";
                    }
                
                    if (colorAnalyzer.isProcessingSuccess)
                    {
                        anchorRotationText.text = "Color detected: " + colorAnalyzer.dominantColor;
                        return;
                    }
                
                    if (Input.touchCount == 2)
                    {
                        var touch = Input.GetTouch(0);
                        if (touch.phase != TouchPhase.Began) 
                            return;
                        
                        anchorRotationText.text = "Capturing image";
                        colorAnalyzer.TryCaptureImage();
                    }
                    return;
                }
                case 4:
                {
                    if (previousTestingFunctionalityIdx != _currentTestingFunctionalityIdx)
                    {
                        _planeWasAdded = false;
                    }
                
                    if (!_planeWasAdded)
                    {
                        raycastManager.Raycast(_screenCenter, _planeHits, TrackableType.Planes);
                        if (_planeHits.Count != 0)
                        {
                            anchorRotationText.text = "Plane found";

                            _planeWasAdded = true;
                            planeScanTip.SetActive(false);
                        }
                        else
                        {
                            anchorRotationText.text = "No plane, showing tip";
                            planeScanTip.SetActive(true);
                        }
                        return;
                    }
                    return;
                }
                default:
                    return;
            }
        }

        private void SetPrefabBasedOnColor(Color targetColor)
        {
            if (targetColor == Color.red)
            {
                prefab = prefabs[0];
            }
            else if (targetColor == Color.green)
            {
                prefab = prefabs[1];
            }
            else if (targetColor == Color.blue)
            {
                prefab = prefabs[2];
            }
        }

        private bool CheckPhoneAlignedWithCrosshair()
        {
            Quaternion rotation = Input.gyro.attitude;
            Vector2 angles = NormalizeRotationAngles(rotation.eulerAngles);
            anchorRotationText.text = angles.ToString();

            var xError = Mathf.Abs(angles.x);
            var yError = Mathf.Abs(angles.y);

            var currentAimingThreshold = crosshairAimingThreshold;

            if (TryGetParamValue(inputFloats, out var val))
            {
                currentAimingThreshold = val[1] == 0 ? crosshairAimingThreshold : val[1];
            }
            
            var isXCaptured = xError < currentAimingThreshold;
            var isYCaptured = yError < currentAimingThreshold;
            
            _horizontalLineRotation.z = isXCaptured ? 0: -angles.x; 
            _verticalLineRotation.z = isYCaptured ? 0: -angles.y;

            horizontalLineImage.color = isXCaptured ? _targetCapturedColor : _defCrosshairColor;
            verticalLineImage.color = isYCaptured ? _targetCapturedColor : _defCrosshairColor;

            horizontalLine.rotation = Quaternion.Euler(_horizontalLineRotation);
            verticalLine.rotation = Quaternion.Euler(_verticalLineRotation);
            
            return isXCaptured && isYCaptured;
        }

        private bool CheckPhoneApproximatelyHorizontal()
        {
            Quaternion rotation = Input.gyro.attitude;
            Vector2 angles = NormalizeRotationAngles(rotation.eulerAngles);
               
            anchorRotationText.text = angles.ToString();

            var xError = Mathf.Abs(angles.x);
            var yError = Mathf.Abs(angles.y);

            var currentThreshold = horizontalThreshold;

            if (TryGetParamValue(inputFloats, out var val))
            {
                currentThreshold = val[1] == 0 ? horizontalThreshold : val[1];
            }

            var isXHorizontal = xError < currentThreshold;
            var isYHorizontal = yError < currentThreshold;
            
            return isXHorizontal && isYHorizontal;
        }
        
        void CreateAnchor(ARRaycastHit hit)
        {
            if (anchorManager.descriptor.supportsTrackableAttachments && hit.trackable is ARPlane plane)
            {
                AttachAnchorToTrackable(plane, hit);
            }
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
        
        void AttachAnchorToTrackable(ARPlane plane, ARRaycastHit hit)
        {
            if (_currentAnchor)
            {
                DestroyImmediate(_currentAnchor.gameObject);
            }
            _currentAnchor = anchorManager.AttachAnchor(plane, hit.pose);
            
            Vector3 offset = _phoneTransform.up * 0.3f;
            
            Quaternion rotation = prefab.transform.rotation;
            if (TryGetParamValue(inputFloats, out var val))
            {
                offset += _currentAnchor.transform.up * val[0];
                if (val[1] != 0)
                {
                    rotation = Quaternion.Euler(0, val[1], 0);
                }
            }
            
            if (_currentPrefab)
            {
                DestroyImmediate(_currentPrefab);
            }

            _currentPrefab = Instantiate(prefab, _currentAnchor.transform.position + offset, rotation,
                _currentAnchor.transform);
            
            _currentPrefab.transform.forward = _phoneTransform.up;
            _currentPrefab.transform.Rotate(_currentAnchor.transform.up, 180);
        }

        private bool TryGetParamValue(TMP_InputField paramField, out float[] val)
        {
            val = new float[]{0,0,0,0,0};

            if (paramField)
            {
                // Split the string by commas
                string[] parts = paramField.text.Split(',');
                if (string.IsNullOrEmpty(parts[0]))
                {
                    return false;
                }
                for (int i = 0; i < parts.Length; i++)
                {
                    val[i] = float.Parse(parts[i], CultureInfo.InvariantCulture);
                }
                
                return true;
            }
            
            return false;
        }
    }
    
    // private void SetColorAnalyzerParams()
    // {
    //     if (TryGetParamValue(inputFloats, out var val2))
    //     {
    //         var nearGreyThreshold = val2[0];
    //         var colorDominanceThreshold = val2[1];
    //         var percentagePixelCountThreshold = val2[2];
    //         var useDominanceMetricWithoutThreshold = val2[3] > 0 ? 1 : 0;
    //         
    //         colorAnalyzer.NearGreyThreshold = (int)nearGreyThreshold;
    //         colorAnalyzer.ColorDominanceThreshold = (int)colorDominanceThreshold;
    //         colorAnalyzer.PercentagePixelCountThreshold = percentagePixelCountThreshold;
    //         colorAnalyzer.useDominanceMetricWithoutThreshold = useDominanceMetricWithoutThreshold;
    //     }
    // }
}