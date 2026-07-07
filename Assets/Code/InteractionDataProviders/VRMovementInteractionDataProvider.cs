using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR;
using CommonUsages = UnityEngine.XR.CommonUsages;
using InputDevice = UnityEngine.XR.InputDevice;

namespace Code
{
    public class VRMovementInteractionDataProvider : MovementInteractionProviderBase
    {
        [Header("Input Actions")]
        [SerializeField] private InputActionReference rotateAroundXAction;
        [SerializeField] private InputActionReference singleTouchAction;
        [SerializeField] private InputActionReference doubleTouchAction;

        // OpenXR doesn't populate CommonUsages.deviceAcceleration, so we derive shake
        // from deviceVelocity: a burst is a spike of the velocity above its low-passed
        // baseline. Several bursts inside a sliding window count as a shake.
        [Header("Shake Detection")]
        [SerializeField] private float burstEnterThreshold = 4f;   // dSq to register a burst
        [SerializeField] private float burstExitThreshold = 1f;    // dSq to release the burst
        [SerializeField] private float burstWindow = 0.7f;         // sliding window in seconds
        [SerializeField] private int minBurstsForShake = 3;
        [SerializeField] private float shakeCooldown = 0.35f;
        [SerializeField] private float lowPassTimeConstant = 0.5f;

        private bool _burstActiveL;
        private bool _burstActiveR;
        private readonly Queue<float> _burstTimesL = new Queue<float>();
        private readonly Queue<float> _burstTimesR = new Queue<float>();

        private Vector3 _lowPassLeft;
        private Vector3 _lowPassRight;
        private bool _leftPrimed;
        private bool _rightPrimed;
        private InputDevice _leftDevice;
        private InputDevice _rightDevice;
        private float _lastShakeTime = float.NegativeInfinity;

        private void OnEnable()
        {
            if (rotateAroundXAction != null && rotateAroundXAction.action != null)
            {
                rotateAroundXAction.action.Enable();
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

            if (rotateAroundXAction != null && rotateAroundXAction.action != null)
            {
                rotateAroundXAction.action.Disable();
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

        protected override void UpdateDeviceTiltAngle()
        {
            var rotNormalized = NormalizeRotationAngles(camTr.rotation.eulerAngles);
            SignedTiltZ01 = Mathf.Clamp(rotNormalized.z, -maxTilt, maxTilt) / maxTilt;
            TiltZ01 = Mathf.Abs(SignedTiltZ01);

            // Negate so positive stick-right maps to negative SignedTiltY01, matching the
            // sign produced by the AR provider (NormalizeRotationAngles multiplies by -1).
            var stickX = 0f;
            if (rotateAroundXAction != null && rotateAroundXAction.action != null)
            {
                stickX = rotateAroundXAction.action.ReadValue<Vector2>().x;
            }
            SignedTiltY01 = Mathf.Clamp(-stickX, -1f, 1f);

            if (isDebugInfoShown && cameraPosRotTxt)
            {
                cameraPosRotTxt.text = $"HMD rot: {rotNormalized}\nSignedTiltZ01: {SignedTiltZ01:F2}\nStickX: {stickX:F2}" +
                                       $"\nSignedTiltY01: {SignedTiltY01:F2}";
            }
        }

        protected override void UpdateTouchStatus() {}

        protected override void UpdateShakeStatus()
        {
            EnsureDevices();

            Vector3 lv = Vector3.zero;
            Vector3 rv = Vector3.zero;

            var hasLeft = _leftDevice.isValid &&
                          _leftDevice.TryGetFeatureValue(CommonUsages.deviceVelocity, out lv);
            var hasRight = _rightDevice.isValid &&
                           _rightDevice.TryGetFeatureValue(CommonUsages.deviceVelocity, out rv);

            var alpha = 1f - Mathf.Exp(-Time.deltaTime / Mathf.Max(lowPassTimeConstant, 1e-4f));

            var deltaSqL = 0f;
            if (hasLeft)
            {
                if (!_leftPrimed)
                {
                    _lowPassLeft = lv;
                    _leftPrimed = true;
                }
                else
                {
                    _lowPassLeft = Vector3.Lerp(_lowPassLeft, lv, alpha);
                }
                deltaSqL = (lv - _lowPassLeft).sqrMagnitude;
            }

            var deltaSqR = 0f;
            if (hasRight)
            {
                if (!_rightPrimed)
                {
                    _lowPassRight = rv;
                    _rightPrimed = true;
                }
                else
                {
                    _lowPassRight = Vector3.Lerp(_lowPassRight, rv, alpha);
                }
                deltaSqR = (rv - _lowPassRight).sqrMagnitude;
            }

            UpdateBurst(deltaSqL, ref _burstActiveL, _burstTimesL);
            UpdateBurst(deltaSqR, ref _burstActiveR, _burstTimesR);

            var shakingL = _burstTimesL.Count >= minBurstsForShake;
            var shakingR = _burstTimesR.Count >= minBurstsForShake;

            if (!shakingL && !shakingR)
                return;

            if (Time.time - _lastShakeTime < shakeCooldown)
                return;
            
            _lastShakeTime = Time.time;

            // Clear histories so we don't immediately re-fire on the tail end of the same shake.
            _burstTimesL.Clear();
            _burstTimesR.Clear();

            ShakeEvent.Invoke();

            if (isDebugInfoShown && shakeText)
                shakeText.text = "Shake event detected at time " + Time.time;
        }

        private void UpdateBurst(float deltaSq, ref bool active, Queue<float> times)
        {
            while (times.Count > 0 && Time.time - times.Peek() > burstWindow)
            {
                times.Dequeue();
            }

            if (!active && deltaSq > burstEnterThreshold)
            {
                active = true;
                times.Enqueue(Time.time);
            }
            else if (active && deltaSq < burstExitThreshold)
            {
                active = false;
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
