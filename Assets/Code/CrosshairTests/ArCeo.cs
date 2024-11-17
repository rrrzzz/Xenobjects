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
        public ARAnchorManager anchorManager;
        public ARRaycastManager raycastManager;
        public ARPlaneManager planeManager;
        public GameObject crosshair;
        public GameObject prefab;
        public GameObject[] prefabs;
        public TMP_InputField offsetText;
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
        private float _checkTimeoutAfterSpawn = 5;
        private bool _isTargetFound;

        private void Start()
        {
            _phoneTransform = Camera.main.transform;
            Input.gyro.enabled = true;
            planeManager.planesChanged += HandlePlanesChanged; 
            float centerX = Screen.width / 2f;
            float centerY = Screen.height / 2f;

            var lineLength = Mathf.Min(centerX, centerY);
            var lineWidth = verticalLine.rect.width;
            
            horizontalLine.sizeDelta = new Vector2(lineLength, lineWidth);
            verticalLine.sizeDelta = new Vector2(lineWidth, lineLength);
            
            _screenCenter = new Vector2(centerX, centerY);
            _horizontalCheckStartTime = Time.realtimeSinceStartup;
        }

        private void HandlePlanesChanged(ARPlanesChangedEventArgs args)
        {
            if (args.added.Count == 0 || _planeWasAdded) return;
            
            _planeWasAdded = true;
            crosshair.SetActive(true);
        }

        private void Update()
        {
            if (colorAnalyzer.isProcessingImage)
            {
                return;
            }
            
            if (Time.realtimeSinceStartup - _horizontalCheckStartTime > horizontalCheckInterval 
                && !_checkingIfSeeingTarget && !_isTargetFound)
            {
                _horizontalCheckStartTime = Time.realtimeSinceStartup;
                if (!CheckPhoneHorizontal())
                {
                    return;
                }
                Debug.Log("Phone is roughly horizontal");
                colorAnalyzer.TryCaptureImage();
                _checkingIfSeeingTarget = true;
            }

            if (_checkingIfSeeingTarget && colorAnalyzer.isProcessingSuccess)
            {
                var color = colorAnalyzer.crossColor;
                if (color != Color.clear)
                {
                    Debug.Log(color + " color detected");
                    colorAnalyzer.isProcessingSuccess = false;
                    _checkingIfSeeingTarget = false;
                    _isTargetFound = true;
                }
                
                colorAnalyzer.isProcessingSuccess = false;
                _checkingIfSeeingTarget = false;
            }

            if (_isTargetFound && !_planeWasAdded)
            {
                //TODO: add instructions to move phone to create plane before showing crosshair
                // shoot a ray to see if there is a plane underneath and if not show instructions
                // think how to visualize plane and if it's needed at all
                return;
            }
            
            if (!_planeWasAdded || !_isTargetFound) return;
            
            if (!CheckPhoneAlignedWithCrosshair())
            {
                return;
            }
            
            // if (!_planeWasAdded) return;

            if (Input.touchCount == 1 && !colorAnalyzer.isProcessingImage)
            {
                colorAnalyzer.TryCaptureImage();
                return;
            }
            
            if (colorAnalyzer.isProcessingImage) return;

            if (!colorAnalyzer.isProcessingSuccess) return;
            
            colorAnalyzer.isProcessingSuccess = false;
            
            var hits = new List<ARRaycastHit>();
            raycastManager.Raycast(_screenCenter, hits, TrackableType.Planes);

            if (hits.Count != 0)
            {
                if (colorAnalyzer.crossColor != Color.clear)
                {
                    SetPrefabBasedOnColor(colorAnalyzer.crossColor);
                    CreateAnchor(hits[0]);
                }
                
                _horizontalCheckStartTime = Time.realtimeSinceStartup - _checkTimeoutAfterSpawn;
                _isTargetFound = false;
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

            var isXCaptured = xError < crosshairAimingThreshold;
            var isYCaptured = yError < crosshairAimingThreshold;
            
            _horizontalLineRotation.z = isXCaptured ? 0: -angles.x; 
            _verticalLineRotation.z = isYCaptured ? 0: -angles.y;

            horizontalLineImage.color = isXCaptured ? _targetCapturedColor : _defCrosshairColor;
            verticalLineImage.color = isYCaptured ? _targetCapturedColor : _defCrosshairColor;

            horizontalLine.rotation = Quaternion.Euler(_horizontalLineRotation);
            verticalLine.rotation = Quaternion.Euler(_verticalLineRotation);
            
            return isXCaptured && isYCaptured;
        }

        private bool CheckPhoneHorizontal()
        {
            Quaternion rotation = Input.gyro.attitude;
            Vector2 angles = NormalizeRotationAngles(rotation.eulerAngles);
               
            anchorRotationText.text = angles.ToString();

            var xError = Mathf.Abs(angles.x);
            var yError = Mathf.Abs(angles.y);

            var isXHorizontal = xError < horizontalThreshold;
            var isYHorizontal = yError < horizontalThreshold;
            
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
            if (TryGetParamValue(offsetText, out var val))
            {
                offset += _currentAnchor.transform.up * val[0];
                if (val[1] != 0)
                {
                    rotation = Quaternion.Euler(0, val[1], 0);
                }
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
}