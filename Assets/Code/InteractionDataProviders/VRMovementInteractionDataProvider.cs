using System.Collections.Generic;
using Code.VR;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;
using UnityEngine.XR;
using CommonUsages = UnityEngine.XR.CommonUsages;
using InputDevice = UnityEngine.XR.InputDevice;

namespace Code
{
    /// <summary>
    /// Quest / OpenXR provider. Phone tilt becomes head roll, phone rotation (the Object 1
    /// puzzle) becomes the left stick, phone shake becomes a shake of either controller and the
    /// screen taps become controller buttons.
    /// </summary>
    public class VRMovementInteractionDataProvider : MovementInteractionProviderBase
    {
        public enum StickRotationMode
        {
            // Deflection turns the puzzle at a fixed rate and the angle holds on release,
            // like keeping a rotated phone in place.
            Rate,
            // Deflection maps straight to the angle; releasing the stick snaps it back.
            Absolute
        }

        [Header("Input Actions")]
        [FormerlySerializedAs("rotateAroundXAction")]
        [SerializeField] private InputActionReference puzzleRotationAction;
        [SerializeField] private InputActionReference singleTouchAction;
        [SerializeField] private InputActionReference doubleTouchAction;
        [Tooltip("Shake fallback when XR InputDevices report no velocity (XR Device Simulator, tests).")]
        [SerializeField] private InputActionReference leftHandPositionAction;
        [SerializeField] private InputActionReference rightHandPositionAction;

        [Header("Path Tracing")]
        [Tooltip("Controllers that can trace the Object 1 tentacle path; the head always can.")]
        [SerializeField] private Transform leftHand;
        [SerializeField] private Transform rightHand;

        [Header("Puzzle Rotation")]
        [SerializeField] private StickRotationMode stickRotationMode = StickRotationMode.Rate;
        [Tooltip("Puzzle degrees per second at full stick deflection (Rate mode).")]
        [SerializeField] private float puzzleRotationSpeed = 90f;

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

        private readonly ShakeDetector _shakeDetector = new ShakeDetector(ShakeDetector.Settings.Default);
        private readonly StickRateIntegrator _puzzleYaw = new StickRateIntegrator();
        private readonly HandVelocity _leftHand = new HandVelocity(XRNode.LeftHand);
        private readonly HandVelocity _rightHand = new HandVelocity(XRNode.RightHand);
        private Transform[] _pathTrackers;

        /// <summary>Live shake signal for the debug HUD.</summary>
        public ShakeDetector Shake => _shakeDetector;

        // The phone traced the path in the visitor's hand. Here the path loops around the object
        // at podium height, out of reach of a standing head, so either hand can trace it too.
        public override Transform[] PathTrackers => _pathTrackers ??= BuildPathTrackers();

        /// <summary>True when the controllers' velocity comes from XR InputDevices (a real headset).</summary>
        public bool IsUsingDeviceVelocity => _leftHand.IsUsingDevice || _rightHand.IsUsingDevice;

        private void OnEnable()
        {
            EnableAction(puzzleRotationAction);
            EnableAction(leftHandPositionAction);
            EnableAction(rightHandPositionAction);

            if (TryGetAction(singleTouchAction, out var singleTouch))
            {
                singleTouch.performed += OnSingleTouchPerformed;
                singleTouch.Enable();
            }

            if (TryGetAction(doubleTouchAction, out var doubleTouch))
            {
                doubleTouch.performed += OnDoubleTouchPerformed;
                doubleTouch.Enable();
            }
        }

        // The actions are shared by every exhibit's provider, so only unsubscribe here:
        // disabling them would silence the other providers. The rig's InputActionManager
        // owns enabling and disabling.
        private void OnDisable()
        {
            if (TryGetAction(singleTouchAction, out var singleTouch))
                singleTouch.performed -= OnSingleTouchPerformed;

            if (TryGetAction(doubleTouchAction, out var doubleTouch))
                doubleTouch.performed -= OnDoubleTouchPerformed;
        }

        private void OnSingleTouchPerformed(InputAction.CallbackContext _)
        {
            if (!IsFocused)
                return;

            if (isDebugInfoShown && tltTxt)
            {
                tltTxt.text = "Single Touch at: " + Time.time;
            }
            SingleTouchEvent.Invoke();
        }

        private void OnDoubleTouchPerformed(InputAction.CallbackContext _)
        {
            if (!IsFocused)
                return;

            if (isDebugInfoShown && tltTxt)
            {
                tltTxt.text = "Double Touch at: " + Time.time;
            }
            DoubleTouchEvent.Invoke();
        }

        public override void SetPuzzleEnteredRotation()
        {
            base.SetPuzzleEnteredRotation();

            // The puzzle measures rotation from the moment of lock-on, like the phone's yaw
            // relative to its heading at that moment.
            _puzzleYaw.Reset();
            SignedTiltY01 = 0;
        }

        protected override void OnFocusChanged(bool isFocused)
        {
            _shakeDetector.Reset();
            _leftHand.ResetFallback();
            _rightHand.ResetFallback();
            _puzzleYaw.Reset();
        }

        protected override void UpdateDeviceTiltAngle()
        {
            var rotNormalized = NormalizeRotationAngles(camTr.rotation.eulerAngles);
            SignedTiltZ01 = Mathf.Clamp(rotNormalized.z, -maxTilt, maxTilt) / maxTilt;
            TiltZ01 = Mathf.Abs(SignedTiltZ01);

            // Negate so positive stick-right maps to negative SignedTiltY01, matching the
            // sign produced by the AR provider (NormalizeRotationAngles multiplies by -1).
            var stickX = 0f;
            if (TryGetAction(puzzleRotationAction, out var puzzleRotation))
            {
                stickX = puzzleRotation.ReadValue<Vector2>().x;
            }

            if (stickRotationMode == StickRotationMode.Rate)
            {
                // SignedTiltY01 * 180 is the puzzle angle, so the rate is in units of 180°.
                SignedTiltY01 = _puzzleYaw.Step(-stickX, puzzleRotationSpeed / 180f, Time.deltaTime);
            }
            else
            {
                SignedTiltY01 = Mathf.Clamp(-stickX, -1f, 1f);
            }

            if (isDebugInfoShown && cameraPosRotTxt)
            {
                cameraPosRotTxt.text = $"HMD rot: {rotNormalized}\nSignedTiltZ01: {SignedTiltZ01:F2}\nStickX: {stickX:F2}" +
                                       $"\nSignedTiltY01: {SignedTiltY01:F2}";
            }
        }

        protected override void UpdateTouchStatus()
        {
        }

        protected override void UpdateShakeStatus()
        {
            _shakeDetector.Config = new ShakeDetector.Settings
            {
                burstEnterThreshold = burstEnterThreshold,
                burstExitThreshold = burstExitThreshold,
                burstWindow = burstWindow,
                minBurstsForShake = minBurstsForShake,
                cooldown = shakeCooldown,
                lowPassTimeConstant = lowPassTimeConstant
            };

            var deltaTime = Time.deltaTime;
            var hasLeft = _leftHand.TryGetVelocity(GetAction(leftHandPositionAction), deltaTime, out var leftVelocity);
            var hasRight = _rightHand.TryGetVelocity(GetAction(rightHandPositionAction), deltaTime, out var rightVelocity);

            if (_leftHand.ConsumeDeviceAcquired())
                _shakeDetector.ResetHand(true);

            if (_rightHand.ConsumeDeviceAcquired())
                _shakeDetector.ResetHand(false);

            if (!_shakeDetector.Step(Time.time, deltaTime, hasLeft, leftVelocity, hasRight, rightVelocity))
                return;

            ShakeEvent.Invoke();

            if (isDebugInfoShown && shakeText)
                shakeText.text = "Shake event detected at time " + Time.time;
        }

        private Transform[] BuildPathTrackers()
        {
            var trackers = new List<Transform> { camTr };
            if (leftHand)
                trackers.Add(leftHand);

            if (rightHand)
                trackers.Add(rightHand);

            return trackers.ToArray();
        }

        private static void EnableAction(InputActionReference reference)
        {
            if (TryGetAction(reference, out var action))
                action.Enable();
        }

        private static bool TryGetAction(InputActionReference reference, out InputAction action)
        {
            action = reference != null ? reference.action : null;
            return action != null;
        }

        private static InputAction GetAction(InputActionReference reference)
        {
            return reference != null ? reference.action : null;
        }

        /// <summary>
        /// Velocity of one controller. Prefers XR InputDevices, which OpenXR fills on Quest and
        /// over Link; falls back to differentiating the Input System device position, which is
        /// all the XR Device Simulator and test devices provide. Both are in tracking space, so
        /// stick locomotion never looks like a shake.
        /// </summary>
        private sealed class HandVelocity
        {
            private readonly XRNode _node;
            private InputDevice _device;
            private Vector3 _prevPosition;
            private bool _hasPrevPosition;
            private bool _isDeviceAcquired;

            public HandVelocity(XRNode node)
            {
                _node = node;
            }

            public bool IsUsingDevice { get; private set; }

            public bool TryGetVelocity(InputAction positionAction, float deltaTime, out Vector3 velocity)
            {
                if (!_device.isValid)
                {
                    _device = InputDevices.GetDeviceAtXRNode(_node);
                    _isDeviceAcquired |= _device.isValid;
                }

                if (_device.isValid && _device.TryGetFeatureValue(CommonUsages.deviceVelocity, out velocity))
                {
                    IsUsingDevice = true;
                    _hasPrevPosition = false;
                    return true;
                }

                IsUsingDevice = false;
                velocity = Vector3.zero;
                if (positionAction == null || positionAction.controls.Count == 0 || deltaTime <= 0f)
                {
                    _hasPrevPosition = false;
                    return false;
                }

                var position = positionAction.ReadValue<Vector3>();
                if (!_hasPrevPosition)
                {
                    _prevPosition = position;
                    _hasPrevPosition = true;
                    return false;
                }

                velocity = (position - _prevPosition) / deltaTime;
                _prevPosition = position;
                return true;
            }

            /// <summary>True once after a controller was (re)acquired, so its filter re-primes.</summary>
            public bool ConsumeDeviceAcquired()
            {
                var wasAcquired = _isDeviceAcquired;
                _isDeviceAcquired = false;
                return wasAcquired;
            }

            public void ResetFallback()
            {
                _hasPrevPosition = false;
            }
        }
    }
}
