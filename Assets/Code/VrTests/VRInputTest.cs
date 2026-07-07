using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;
using UnityEngine.XR;
using CommonUsages = UnityEngine.XR.CommonUsages;
using InputDevice = UnityEngine.XR.InputDevice;

public class VRInputTest : MonoBehaviour
{
    public Transform arObjectTr;

    [Header("Debug")]
    [SerializeField] private bool isDebugInfoShown;
    [SerializeField] private TMP_Text shakeText;
    [SerializeField] private TMP_Text cameraPosRotTxt;
    [SerializeField] private TMP_Text distanceToObjTxt;
    [SerializeField] private TMP_Text objPosTxt;
    [FormerlySerializedAs("titlTxt")] [SerializeField] private TMP_Text tiltTxt;
    [SerializeField] private TMP_Text movedTxt;

    [Header("Tilt / Distance / Movement")]
    [SerializeField] private float maxTilt = 60f;
    [SerializeField] private float maxDistance = 3f;
    [SerializeField] private float minDistance = 1.5f;
    [SerializeField] private float movementTrackingInterval = 1f;
    [SerializeField] private float movementThreshold = 0.2f;

    [Header("Input Actions")]
    [SerializeField] private InputActionReference leftStickRotate;
    [SerializeField] private InputActionReference aButtonDoubleTouch;
    [SerializeField] private InputActionReference bButtonSingleTouch;

    [Header("Shake Detection")]
    [SerializeField] private float burstEnterThreshold = 4f;   // dSq to register a burst
    [SerializeField] private float burstExitThreshold = 1f;    // dSq to release the burst
    [SerializeField] private float burstWindow = 0.7f;         // sliding window in seconds
    [SerializeField] private int minBurstsForShake = 3;  
    [SerializeField] private float shakeCooldown = 0.35f;
    [SerializeField] private float lowPassTimeConstant = 0.5f;

    public float DistanceToArObject01 { get; set; }
    public float DistanceToArObjectRaw { get; set; }
    public float TiltZ01 { get; set; }
    public float SignedTiltZ01 { get; set; }
    public float SignedTiltY01 { get; set; }
    public bool IsMoving { get; set; }
    public float MovementDuration { get; set; }
    public float IdleDuration { get; set; }
    public UnityEvent DoubleTouchEvent { get; } = new UnityEvent();
    public UnityEvent SingleTouchEvent { get; } = new UnityEvent();
    public UnityEvent ShakeEvent { get; } = new UnityEvent();

    [HideInInspector] public Transform camTr;

    private bool _burstActiveL, _burstActiveR;
    private readonly Queue<float> _burstTimesL = new Queue<float>();
    private readonly Queue<float> _burstTimesR = new Queue<float>();
    
    private float _movementStateStartTime;
    private Vector3 _camPrevPosition;
    private float _prevTime;

    private Vector3 _lowPassLeft;
    private Vector3 _lowPassRight;
    private bool _leftPrimed;
    private bool _rightPrimed;
    private InputDevice _leftDevice;
    private InputDevice _rightDevice;
    private float _lastShakeTime = float.NegativeInfinity;

    private void Awake()
    {
        camTr = Camera.main.transform;
        _camPrevPosition = camTr.position;
    }

    private void OnEnable()
    {
        if (leftStickRotate != null && leftStickRotate.action != null)
        {
            leftStickRotate.action.Enable();
        }

        if (bButtonSingleTouch != null && bButtonSingleTouch.action != null)
        {
            bButtonSingleTouch.action.performed += OnSingleTouchPerformed;
            bButtonSingleTouch.action.Enable();
        }

        if (aButtonDoubleTouch != null && aButtonDoubleTouch.action != null)
        {
            aButtonDoubleTouch.action.performed += OnDoubleTouchPerformed;
            aButtonDoubleTouch.action.Enable();
        }
    }

    private void OnDisable()
    {
        if (bButtonSingleTouch != null && bButtonSingleTouch.action != null)
        {
            bButtonSingleTouch.action.performed -= OnSingleTouchPerformed;
            bButtonSingleTouch.action.Disable();
        }

        if (aButtonDoubleTouch != null && aButtonDoubleTouch.action != null)
        {
            aButtonDoubleTouch.action.performed -= OnDoubleTouchPerformed;
            aButtonDoubleTouch.action.Disable();
        }

        if (leftStickRotate != null && leftStickRotate.action != null)
        {
            leftStickRotate.action.Disable();
        }
    }

    private void Update()
    {
        UpdateMovementStatus();
        UpdateDistanceToObject();
        UpdatePhoneTiltAngle();
        UpdateTouchStatus();
        UpdateShakeStatus();
    }

    private void OnSingleTouchPerformed(InputAction.CallbackContext _)
    {
        if (isDebugInfoShown && tiltTxt)
        {
            tiltTxt.text = "Single Touch at: " + Time.time;
        }
        Debug.Log("Single touch pressed");
        SingleTouchEvent.Invoke();
    }

    private void OnDoubleTouchPerformed(InputAction.CallbackContext _)
    {
        if (isDebugInfoShown && tiltTxt)
        {
            tiltTxt.text = "Double Touch at: " + Time.time;
        }
        DoubleTouchEvent.Invoke();
    }

    private void UpdateMovementStatus()
    {
        var movementStateDuration = Time.realtimeSinceStartup - _movementStateStartTime;
        if (IsMoving)
        {
            MovementDuration = movementStateDuration;
        }
        else
        {
            IdleDuration = movementStateDuration;
        }

        if (!(Time.realtimeSinceStartup - _prevTime > movementTrackingInterval))
            return;

        var movementDist = Vector3.Distance(camTr.position, _camPrevPosition);
        _camPrevPosition = camTr.position;
        _prevTime = Time.realtimeSinceStartup;

        var currentMovementStatus = movementDist > movementThreshold;
        if (currentMovementStatus == IsMoving)
            return;
        IsMoving = currentMovementStatus;

        if (isDebugInfoShown && movedTxt)
        {
            movedTxt.text = IsMoving ? "MOVING" : "IDLE";
        }

        IdleDuration = MovementDuration = 0;

        _movementStateStartTime = Time.realtimeSinceStartup;
    }

    private void UpdateDistanceToObject()
    {
        if (!arObjectTr)
            return;

        DistanceToArObjectRaw = Vector3.Distance(camTr.position, arObjectTr.position);

        if (isDebugInfoShown && objPosTxt && distanceToObjTxt)
        {
            objPosTxt.text = "obj pos: " + arObjectTr.position;
            distanceToObjTxt.text = "Dist to obj: " + DistanceToArObjectRaw;
        }
        else if (isDebugInfoShown)
        {
            Debug.Log("Dist to obj: " + DistanceToArObjectRaw);
        }

        DistanceToArObject01 = 1 - Mathf.InverseLerp(minDistance, maxDistance, DistanceToArObjectRaw);
    }

    private void UpdatePhoneTiltAngle()
    {
        var rotNormalized = NormalizeRotationAngles(camTr.rotation.eulerAngles);
        SignedTiltZ01 = Mathf.Clamp(rotNormalized.z, -maxTilt, maxTilt) / maxTilt;
        TiltZ01 = Mathf.Abs(SignedTiltZ01);

        var stickX = 0f;
        if (leftStickRotate != null && leftStickRotate.action != null)
        {
            stickX = leftStickRotate.action.ReadValue<Vector2>().x;
        }
        SignedTiltY01 = Mathf.Clamp(-stickX, -1f, 1f);

        if (isDebugInfoShown && cameraPosRotTxt)
        {
            cameraPosRotTxt.text = $"HMD rot: {rotNormalized}\nSignedTiltZ01: {SignedTiltZ01:F2}\nStickX: {stickX:F2}" +
                                   $"\nSignedTiltY01: {SignedTiltY01:F2}";
        }
    }

    private void UpdateTouchStatus()
    {
    }
    
    private void UpdateShakeStatus()
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
            if (!_leftPrimed) { _lowPassLeft = lv; _leftPrimed = true; }
            else _lowPassLeft = Vector3.Lerp(_lowPassLeft, lv, alpha);
            deltaSqL = (lv - _lowPassLeft).sqrMagnitude;
        }
    
        var deltaSqR = 0f;
        if (hasRight)
        {
            if (!_rightPrimed) { _lowPassRight = rv; _rightPrimed = true; }
            else _lowPassRight = Vector3.Lerp(_lowPassRight, rv, alpha);
            deltaSqR = (rv - _lowPassRight).sqrMagnitude;
        }
    
        UpdateBurst(deltaSqL, ref _burstActiveL, _burstTimesL);
        UpdateBurst(deltaSqR, ref _burstActiveR, _burstTimesR);
    
        var shakingL = _burstTimesL.Count >= minBurstsForShake;
        var shakingR = _burstTimesR.Count >= minBurstsForShake;
    
        if (!shakingL && !shakingR)
        {
            Debug.Log($"L burstCount={_burstTimesL.Count}  R burstCount={_burstTimesR.Count}  " +
                      $"dSqL={deltaSqL:F2}  dSqR={deltaSqR:F2}  t={Time.time:F2}");
            return;
        }
    
        if (Time.time - _lastShakeTime < shakeCooldown)
            return;
    
        _lastShakeTime = Time.time;
    
        // Clear histories so we don't immediately re-fire on the tail end of the same shake
        _burstTimesL.Clear();
        _burstTimesR.Clear();
    
        ShakeEvent.Invoke();
    
        if (isDebugInfoShown && shakeText)
            shakeText.text = "Shake event detected at time " + Time.time;
    }
    
    private void UpdateBurst(float deltaSq, ref bool active, Queue<float> times)
    {
        while (times.Count > 0 && Time.time - times.Peek() > burstWindow)
            times.Dequeue();
    
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
    
    
    
    private static Vector3 NormalizeRotationAngles(Vector3 rotation)
    {
        if (rotation.x > 180)
            rotation.x -= 360;

        if (rotation.y > 180)
            rotation.y -= 360;

        if (rotation.z > 180)
            rotation.z -= 360;

        return rotation * -1;
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
