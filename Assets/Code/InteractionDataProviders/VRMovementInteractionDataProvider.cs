using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR;
using CommonUsages = UnityEngine.XR.CommonUsages;
using InputDevice = UnityEngine.XR.InputDevice;

namespace Code
{
    public class VRMovementInteractionDataProvider : MovementInteractionProviderBase
    {
        // Quest reports deviceAcceleration in m/s²; AR's Input.acceleration is in g.
        // We divide by this so shakeThreshold stays comparable to the AR constant (2.0).
        private const float StandardGravity = 9.81f;

        [Header("Input Actions")]
        [SerializeField] private InputActionReference leftStickAction;
        [SerializeField] private InputActionReference singleTouchAction;
        [SerializeField] private InputActionReference doubleTouchAction;

        [Header("Shake Detection")]
        [SerializeField] private float shakeThreshold = 2.0f;
        [SerializeField] private float shakeCooldown = 0.35f;
        [SerializeField] private float lowPassTimeConstant = 0.5f;

        private Vector3 _lowPassLeft;
        private Vector3 _lowPassRight;
        private bool _leftPrimed;
        private bool _rightPrimed;
        private InputDevice _leftDevice;
        private InputDevice _rightDevice;
        private float _lastShakeTime = float.NegativeInfinity;

        private void OnEnable()
        {
            if (leftStickAction != null && leftStickAction.action != null)
            {
                leftStickAction.action.Enable();
            }

            if (singleTouchAction != null && singleTouchAction.action != null)
            {
                singleTouchAction.action.performed += OnSingleTouchPerformed;
                singleTouchAction.action.Enable();
            }

            if (doubleTouchAction != null && doubleTouchAction.action != null)
            {
                doubleTouchAction.action.performed += OnDoubleTouchPerformed;
                doubleTouchAction.action.Enable();
            }
        }

        private void OnDisable()
        {
            if (singleTouchAction != null && singleTouchAction.action != null)
            {
                singleTouchAction.action.performed -= OnSingleTouchPerformed;
                singleTouchAction.action.Disable();
            }

            if (doubleTouchAction != null && doubleTouchAction.action != null)
            {
                doubleTouchAction.action.performed -= OnDoubleTouchPerformed;
                doubleTouchAction.action.Disable();
            }

            if (leftStickAction != null && leftStickAction.action != null)
            {
                leftStickAction.action.Disable();
            }
        }

        private void OnSingleTouchPerformed(InputAction.CallbackContext _)
        {
            if (isDebugInfoShown && tltTxt)
            {
                tltTxt.text = "Single Touch at: " + Time.time;
            }
            SingleTouchEvent.Invoke();
        }

        private void OnDoubleTouchPerformed(InputAction.CallbackContext _)
        {
            if (isDebugInfoShown && tltTxt)
            {
                tltTxt.text = "Double Touch at: " + Time.time;
            }
            DoubleTouchEvent.Invoke();
        }

        protected override void UpdatePhoneTiltAngle()
        {
            var rotNormalized = NormalizeRotationAngles(camTr.rotation.eulerAngles);
            SignedTiltZ01 = Mathf.Clamp(rotNormalized.z, -maxTilt, maxTilt) / maxTilt;
            TiltZ01 = Mathf.Abs(SignedTiltZ01);

            // Negate so positive stick-right maps to negative SignedTiltY01, matching the
            // sign produced by the AR provider (NormalizeRotationAngles multiplies by -1).
            var stickX = 0f;
            if (leftStickAction != null && leftStickAction.action != null)
            {
                stickX = leftStickAction.action.ReadValue<Vector2>().x;
            }
            SignedTiltY01 = Mathf.Clamp(-stickX, -1f, 1f);

            if (isDebugInfoShown && cameraPosRotTxt)
            {
                cameraPosRotTxt.text = $"HMD rot: {rotNormalized}\nSignedTiltZ01: {SignedTiltZ01}\nStickX: {stickX:F2}" +
                                       $"\nSignedTiltY01: {SignedTiltY01}";
            }
        }

        protected override void UpdateTouchStatus()
        {
        }

        protected override void UpdateShakeStatus()
        {
            EnsureDevices();

            Vector3 la = Vector3.zero;
            Vector3 ra = Vector3.zero;

            var hasLeft = _leftDevice.isValid &&
                          _leftDevice.TryGetFeatureValue(CommonUsages.deviceAcceleration, out la);
            var hasRight = _rightDevice.isValid &&
                           _rightDevice.TryGetFeatureValue(CommonUsages.deviceAcceleration, out ra);

            var alpha = 1f - Mathf.Exp(-Time.deltaTime / Mathf.Max(lowPassTimeConstant, 1e-4f));

            var deltaSqL = 0f;
            if (hasLeft)
            {
                la /= StandardGravity;
                if (!_leftPrimed)
                {
                    _lowPassLeft = la; 
                    _leftPrimed = true;
                }
                else
                {
                    _lowPassLeft = Vector3.Lerp(_lowPassLeft, la, alpha);
                }
                deltaSqL = (la - _lowPassLeft).sqrMagnitude;
            }

            var deltaSqR = 0f;
            if (hasRight)
            {
                ra /= StandardGravity;
                if (!_rightPrimed)
                {
                    _lowPassRight = ra; 
                    _rightPrimed = true;
                }
                else
                {
                    _lowPassRight = Vector3.Lerp(_lowPassRight, ra, alpha);
                }
                
                deltaSqR = (ra - _lowPassRight).sqrMagnitude;
            }

            var deltaSq = Mathf.Max(deltaSqL, deltaSqR);
            if (deltaSq < shakeThreshold) return;
            if (Time.time - _lastShakeTime < shakeCooldown) return;

            _lastShakeTime = Time.time;
            ShakeEvent.Invoke();

            if (isDebugInfoShown && shakeText)
            {
                shakeText.text = "Shake event detected at time " + Time.time;
            }
        }

        private void EnsureDevices()
        {
            if (!_leftDevice.isValid)
            {
                _leftDevice = InputDevices.GetDeviceAtXRNode(XRNode.LeftHand);
                _leftPrimed = false;
            }
            if (!_rightDevice.isValid)
            {
                _rightDevice = InputDevices.GetDeviceAtXRNode(XRNode.RightHand);
                _rightPrimed = false;
            }
        }
    }
}
