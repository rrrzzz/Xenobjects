using System.Collections.Generic;
using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using Button = UnityEngine.UI.Button;
using Image = UnityEngine.UI.Image;
using Toggle = UnityEngine.UI.Toggle;

namespace Code
{
    public class ArCeo : MonoBehaviour
    {
        public float checkTimeoutAfterSpawn = 10;
        public ARPlaneManager arPlaneManager;
        public ARAnchorManager anchorManager;
        public ARRaycastManager raycastManager;
        public ARMovementInteractionDataProvider dataProvider;
        public GameObject crosshair;
        public GameObject planeScanTip;
        public GameObject chosenPrefab;
        public GameObject[] prefabs;
        public TMP_InputField inputFloats;
        public float crosshairAimingThreshold = 2;
        public float horizontalThreshold = 10;
        public float horizontalCheckInterval = 1;
        public float crossCheckInterval = .5f;
        public TMP_Text anchorRotationText;
        [FormerlySerializedAs("verticalLine")] public RectTransform crosshairVerticalLine;
        [FormerlySerializedAs("horizontalLine")] public RectTransform crosshairHorizontalLine;
        public Image verticalLineImage;
        public Image horizontalLineImage;
        public ARColorAnalyzer colorAnalyzer;
        public Color firstObjectColor = Color.red;
        public Color secondObjectColor = Color.green;
        public Color thirdObjectColor = Color.blue;
        public Button button1;
        public Button button2;
        public Button button3;
        public Toggle isSpawningViaButtonsToggle;
        public MovementPathVisualizer movementPathVisualizer;
        
        private float _crossCheckStartTime;
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
        private bool _hasCheckedCross;
        private bool _isSpawned;
        private bool _isManualMode;
        private bool _isPhoneHorizontal;
        private bool _isAlignedWithCrosshair;
        private bool _isArPlanesVisible;

        private void Start()
        {
            _phoneTransform = Camera.main.transform;
            Input.gyro.enabled = true;
            float centerX = Screen.width / 2f;
            float centerY = Screen.height / 2f;
        
            var lineLength = Mathf.Min(centerX, centerY);
            var lineWidth = crosshairVerticalLine.rect.width;
            
            crosshairHorizontalLine.sizeDelta = new Vector2(lineLength, lineWidth);
            crosshairVerticalLine.sizeDelta = new Vector2(lineWidth, lineLength);
            
            _screenCenter = new Vector2(centerX, centerY);
            _horizontalCheckStartTime = _crossCheckStartTime = Time.realtimeSinceStartup;
            // button.onClick.AddListener(() =>
            // {
            //     _isManualMode = !_isManualMode;
            // });

            _isManualMode = isSpawningViaButtonsToggle.isOn;
            button1.onClick.AddListener(() => SpawnObjectManually(prefabs[0]));
            button2.onClick.AddListener(() => SpawnObjectManually(prefabs[1]));
            button3.onClick.AddListener(() => SpawnObjectManually(prefabs[2]));
            
            isSpawningViaButtonsToggle.onValueChanged.AddListener(isManualSpawnOn => _isManualMode = isManualSpawnOn);
        }

        private void SpawnObjectManually(GameObject prefab)
        {
            if (!_isManualMode || !_isPhoneHorizontal || !_isAlignedWithCrosshair)
                return;
            
            raycastManager.Raycast(_screenCenter, _planeHits, TrackableType.Planes);

            if (_planeHits.Count == 0) 
                return;
            
            crosshair.SetActive(false);
            
            chosenPrefab = prefab;
            
            CreateAnchor(_planeHits[0]);

            ToggleArPlanesVisibility(false);
            _planeWasAdded = false;
            _isAlignedWithCrosshair = false;
            _isPhoneHorizontal = false;
            _horizontalCheckStartTime = Time.realtimeSinceStartup + checkTimeoutAfterSpawn;
        }

        private void ToggleArPlanesVisibility(bool isVisible)
        {
            if (_isArPlanesVisible == isVisible)
                return;
            
            _isArPlanesVisible = isVisible;
            foreach (ARPlane plane in arPlaneManager.trackables)
            {
                var meshVisualizer = plane.GetComponent<ARPlaneMeshVisualizer>();
                if (meshVisualizer)
                    meshVisualizer.enabled = isVisible;
            }
        }

        //TODO remove crosshair if phone is not horizontal \ doesn't see color
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

            if (_isManualMode)
            {
                ProcessManualSpawn();
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
                        EnableMovementPathVizIfObjectSpawned();
                        crosshair.SetActive(false);
                        return;
                    }
                    movementPathVisualizer.enabled = false;
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
                else
                {
                    crosshair.SetActive(false);
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

            ToggleArPlanesVisibility(true);
            //TODO: Maybe disable plane creation when plane is detected and object is created
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
            
            if (_isManualMode)
            {
                if (Input.touchCount == 1)
                {
                    _hasCheckedCross = true;
                    colorAnalyzer.TryDetermineCenteredAtCross();
                    return;
                }
            }
            
            if (!_isManualMode && Time.realtimeSinceStartup - _crossCheckStartTime >= crossCheckInterval)
            {
                _crossCheckStartTime = Time.realtimeSinceStartup;
                _hasCheckedCross = true;
                colorAnalyzer.TryDetermineCenteredAtCross();
                return;
            }
            
            if (!_hasCheckedCross)
            {
                return;
            }

            if (colorAnalyzer.dominantColor == Color.clear)
            {
                anchorRotationText.text = "No color detected when phone aligned with crosshair, probably moved it away.";
                _isTargetFound = false;
                colorAnalyzer.isProcessingSuccess = false;
                _hasCheckedCross = false;
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
                    ToggleArPlanesVisibility(false);
                }
                
                _horizontalCheckStartTime = Time.realtimeSinceStartup + checkTimeoutAfterSpawn;
                _isTargetFound = false;
            }
            
            colorAnalyzer.dominantColor = Color.clear;
            _planeWasAdded = false;  
            _hasCheckedCross = false;
        }

        private void ProcessManualSpawn()
        {
            if (Time.realtimeSinceStartup - _horizontalCheckStartTime >= horizontalCheckInterval)
            {
                _horizontalCheckStartTime = Time.realtimeSinceStartup;
                _isPhoneHorizontal = CheckPhoneApproximatelyHorizontal();
            }

            if (!_isPhoneHorizontal)
            {
                crosshair.SetActive(false);
                ToggleArPlanesVisibility(false);
                return;
            }
            
            ToggleArPlanesVisibility(true);
            
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
            
            if (!crosshair.activeInHierarchy)
            {
                crosshair.SetActive(true);
            }

            _isAlignedWithCrosshair = CheckPhoneAlignedWithCrosshair();
        }

        private void EnableMovementPathVizIfObjectSpawned()
        {
            if (_currentPrefab)
            {
                movementPathVisualizer.enabled = true;
            }
        }

        private void SetPrefabBasedOnColor(Color targetColor)
        {
            if (targetColor == firstObjectColor)
            {
                chosenPrefab = prefabs[0];
            }
            else if (targetColor == secondObjectColor)
            {
                chosenPrefab = prefabs[1];
            }
            else if (targetColor == thirdObjectColor)
            {
                chosenPrefab = prefabs[2];
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

            // if (TryGetParamValue(inputFloats, out var val))
            // {
            //     currentAimingThreshold = val[1] == 0 ? crosshairAimingThreshold : val[1];
            // }
            //
            var isXCaptured = xError < currentAimingThreshold;
            var isYCaptured = yError < currentAimingThreshold;
            
            _horizontalLineRotation.z = isXCaptured ? 0: -angles.x; 
            _verticalLineRotation.z = isYCaptured ? 0: -angles.y;

            horizontalLineImage.color = isXCaptured ? _targetCapturedColor : _defCrosshairColor;
            verticalLineImage.color = isYCaptured ? _targetCapturedColor : _defCrosshairColor;

            crosshairHorizontalLine.rotation = Quaternion.Euler(_horizontalLineRotation);
            crosshairVerticalLine.rotation = Quaternion.Euler(_verticalLineRotation);
            
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

            // if (TryGetParamValue(inputFloats, out var val))
            // {
            //     currentThreshold = val[1] == 0 ? horizontalThreshold : val[1];
            // }

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
            
            Quaternion rotation = chosenPrefab.transform.rotation;
            // if (TryGetParamValue(inputFloats, out var val))
            // {
            //     offset += _currentAnchor.transform.up * val[0];
            //     if (val[1] != 0)
            //     {
            //         rotation = Quaternion.Euler(0, val[1], 0);
            //     }
            // }

            movementPathVisualizer.enabled = true;
                
            if (_currentPrefab)
            {
                DestroyImmediate(_currentPrefab);
            }
            else
            {
                movementPathVisualizer.Initialize();
            }
            
            movementPathVisualizer.RestartTracking();

            _currentPrefab = Instantiate(chosenPrefab, _currentAnchor.transform.position + offset, rotation,
                _currentAnchor.transform);

            var prefabRotation = 180;
            
            _currentPrefab.transform.forward = _phoneTransform.up;
            _currentPrefab.transform.Rotate(_currentAnchor.transform.up, prefabRotation);
            
            dataProvider.SetArObjectTransform(_currentPrefab.transform, movementPathVisualizer);
        }

        public bool TryGetParamValue(TMP_InputField paramField, out float[] val)
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
        
        private void TestFunctionalityById()
        {
            var previousTestingFunctionalityIdx = _currentTestingFunctionalityIdx;
            // _currentTestingFunctionalityIdx = TryGetParamValue(inputFloats, out var val) ? (int)val[0] : 0;
            
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
    }
}