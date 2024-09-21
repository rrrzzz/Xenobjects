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
        public TMP_InputField offsetText;
        public float threshold = 2;
        public TMP_Text anchorRotationText;
        public RectTransform verticalLine;
        public RectTransform horizontalLine;
        public Button rotateBtn;
        public Image verticalLineImage;
        public Image horizontalLineImage;
        private Vector3 verticalLineRotation = Vector3.zero;
        private Vector3 horizontalLineRotation = Vector3.zero;
        private bool _planeWasAdded;
        private ARAnchor _currentAnchor;
        private GameObject _currentPrefab;
        private Vector2 _screenCenter;
        private bool _isPhoneHorizontal;
        private Color _defCrosshairColor = Color.white;
        private Color _targetCapturedColor = Color.red;
        private Transform _phoneTransform;
        
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
        }

        private void HandlePlanesChanged(ARPlanesChangedEventArgs args)
        {
            if (args.added.Count != 0 && !_planeWasAdded)
            {
                _planeWasAdded = true;
                crosshair.SetActive(true);
            }
        }

        private void Update()
        {
            Quaternion rotation = Input.gyro.attitude;
            Vector2 angles = NormalizeRotationAngles(rotation.eulerAngles);
               
            anchorRotationText.text = angles.ToString();

            var xError = Mathf.Abs(angles.x);
            var yError = Mathf.Abs(angles.y);

            var isXCaptured = xError < threshold;
            var isYCaptured = yError < threshold;
            horizontalLineRotation.z = isXCaptured ? 0: -angles.x; 
            verticalLineRotation.z = isYCaptured ? 0: -angles.y;

            horizontalLineImage.color = isXCaptured ? _targetCapturedColor : _defCrosshairColor;
            verticalLineImage.color = isYCaptured ? _targetCapturedColor : _defCrosshairColor;

            horizontalLine.rotation = Quaternion.Euler(horizontalLineRotation);
            verticalLine.rotation = Quaternion.Euler(verticalLineRotation);

            if (isXCaptured &&
                isYCaptured)
            {
                _isPhoneHorizontal = true;
            }
            else
            {
                _isPhoneHorizontal = false;
            }
            
            if (!_planeWasAdded) return;
            
            if (Input.touchCount != 1) return;
            var touch = Input.GetTouch(0);
            
            if (touch.phase != TouchPhase.Began) 
                return;

            var hits = new List<ARRaycastHit>();
            raycastManager.Raycast(_screenCenter, hits, TrackableType.Planes);

            if (hits.Count != 0)
            {
                CreateAnchor(hits[0]);
            }
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
    }
}