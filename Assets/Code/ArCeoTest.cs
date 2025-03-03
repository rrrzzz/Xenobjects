using TMPro;
using UnityEngine;
using UnityEngine.PlayerLoop;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace Code
{
    public class ArCeoTest : MonoBehaviour
    {
        private const float CheckTimeoutAfterSpawn = 1;
        
        public float touchCheckInterval = .5f;
        public SimpleCharacterMotor characterMotor;
        public GameObject crosshair;
        public GameObject prefab;
        public GameObject[] prefabs;
        public float crosshairAimingThreshold = 2;
        public float horizontalThreshold = 10;
        public float horizontalCheckInterval = 1;
        public TMP_Text anchorRotationText;
        [FormerlySerializedAs("verticalLine")] public RectTransform crosshairVerticalLine;
        [FormerlySerializedAs("horizontalLine")] public RectTransform crosshairHorizontalLine;
        public Image verticalLineImage;
        public Image horizontalLineImage;
        public TestColorAnalyzer colorAnalyzer;
        public Color firstObjectColor = Color.red;
        public Color secondObjectColor = Color.green;
        public Color thirdObjectColor = Color.blue;
        
        private float _touchCheckStartTime;
        private Vector3 _verticalLineRotation = Vector3.zero;
        private Vector3 _horizontalLineRotation = Vector3.zero;
        private GameObject _currentPrefab;
        private readonly Color _defCrosshairColor = Color.white;
        private readonly Color _targetCapturedColor = Color.red;
        private Transform _phoneTransform;
        private bool _isCapturingImage;
        private bool _isFirstCapture;
        private float _horizontalCheckStartTime;
        private bool _checkingIfSeeingTarget;
        private bool _isTargetFound;
        private int _currentTestingFunctionalityIdx;
        private bool _hasTouchedScreen;
        private bool _isSpawned;

        private void Start()
        {
            _phoneTransform = Camera.main.transform;
            float centerX = Screen.width / 2f;
            float centerY = Screen.height / 2f;
        
            var lineLength = Mathf.Min(centerX, centerY);
            var lineWidth = crosshairVerticalLine.rect.width;
            
            crosshairHorizontalLine.sizeDelta = new Vector2(lineLength, lineWidth);
            crosshairVerticalLine.sizeDelta = new Vector2(lineWidth, lineLength);
            _horizontalCheckStartTime = Time.realtimeSinceStartup;
            // crosshair.SetActive(true);

        }

        // private void Update()
        // {
        //     CheckPhoneAlignedWithCrosshair();
        //     if (Input.GetKeyDown(KeyCode.Space))
        //     {
        //         
        //     }
        //     
        // }

        private void Update()
        {
            if (colorAnalyzer.isProcessingImage)
            {
                anchorRotationText.text = "Processing image, " + (_checkingIfSeeingTarget ? "checking if seeing cross" : "");
                return;
            }

            if (crosshair.activeInHierarchy)
            {
                CheckPhoneAlignedWithCrosshair();
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
                    _isTargetFound = true;
                }
                else
                {
                    crosshair.SetActive(false);
                }
                
                colorAnalyzer.isProcessingSuccess = false;
                _checkingIfSeeingTarget = false;
            }
            
            if (!_isTargetFound) return;

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
            if (Time.realtimeSinceStartup - _touchCheckStartTime >= touchCheckInterval)
            {
                _touchCheckStartTime = Time.realtimeSinceStartup;
                _hasTouchedScreen = true;
                colorAnalyzer.TryDetermineCenteredAtCross();
                return;
            }
            
            if (!_hasTouchedScreen)
            {
                return;
            }

            if (colorAnalyzer.dominantColor == Color.clear)
            {
                _isTargetFound = false;
                anchorRotationText.text = "No color detected when phone aligned with crosshair, probably moved it away.";
                colorAnalyzer.isProcessingSuccess = false;
                _hasTouchedScreen = false;
                return;
            }
            
            colorAnalyzer.isProcessingSuccess = false;
            crosshair.SetActive(false);
            
            if (colorAnalyzer.dominantColor != Color.clear)
            {
                SetPrefabBasedOnColor(colorAnalyzer.dominantColor);
                CreateAnchor();
            }
                
            _horizontalCheckStartTime = Time.realtimeSinceStartup + CheckTimeoutAfterSpawn;
            _isTargetFound = false;
            
            colorAnalyzer.dominantColor = Color.clear;
            _hasTouchedScreen = false;
        }
        
        private void SetPrefabBasedOnColor(Color targetColor)
        {
            if (targetColor == firstObjectColor)
            {
                prefab = prefabs[0];
            }
            else if (targetColor == secondObjectColor)
            {
                prefab = prefabs[1];
            }
            else if (targetColor == thirdObjectColor)
            {
                prefab = prefabs[2];
            }
        }

        private bool CheckPhoneAlignedWithCrosshair()
        {
            Vector3 angles = _phoneTransform.rotation.eulerAngles;
            anchorRotationText.text = angles.ToString();

            var xError = Mathf.Abs(angles.x - 90);
            var yError = Mathf.Abs(angles.y - 180);

            var currentAimingThreshold = crosshairAimingThreshold;
            var isXCaptured = xError < currentAimingThreshold;
            var isYCaptured = yError < currentAimingThreshold;
            
            _horizontalLineRotation.z = isXCaptured ? 0: -angles.x; 
            _verticalLineRotation.z = isYCaptured ? 0: -angles.y;

            horizontalLineImage.color = isXCaptured ? colorAnalyzer.dominantColor : _defCrosshairColor;
            verticalLineImage.color = isYCaptured ? colorAnalyzer.dominantColor : _defCrosshairColor;

            crosshairHorizontalLine.rotation = Quaternion.Euler(_horizontalLineRotation);
            crosshairVerticalLine.rotation = Quaternion.Euler(_verticalLineRotation);

            return isXCaptured && isYCaptured;
        }

        private bool CheckPhoneApproximatelyHorizontal()
        {
            Vector3 angles = _phoneTransform.rotation.eulerAngles;
            
            // Debug.Log("phone rotation " + angles);
            // anchorRotationText.text = angles.ToString();

            var xError = Mathf.Abs(angles.x - 90);
            // Debug.Log("xError " + xError);

            var currentThreshold = horizontalThreshold;

            var isXHorizontal = xError < currentThreshold; 
            
            return isXHorizontal;
        }
        
        void CreateAnchor()
        {
            AttachAnchorToTrackable();
        }
        
        void AttachAnchorToTrackable()
        {
            if (_currentPrefab)
            {
                DestroyImmediate(_currentPrefab);
            }

            _currentPrefab = Instantiate(prefab);
            _currentPrefab.transform.forward = _phoneTransform.up;
        }
    }
}