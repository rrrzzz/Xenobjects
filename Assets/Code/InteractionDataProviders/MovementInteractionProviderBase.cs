using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;
using Vector3 = UnityEngine.Vector3;

namespace Code
{
    public abstract class MovementInteractionProviderBase : MonoBehaviour
    {
        public Transform arObjectTr;
        [SerializeField] protected bool isDebugInfoShown;
        [SerializeField] protected float maxTilt = 60;
        [SerializeField] protected float maxTiltY = 60;
        [SerializeField] protected float maxDistance = 3;
        [SerializeField] protected float minDistance = 1.5f;
        [SerializeField] protected float movementTrackingInterval = 1f;
        [SerializeField] protected float movementThreshold = 0.2f;
        
        [SerializeField] protected TMP_Text shakeText;
        [SerializeField] protected TMP_Text cameraPosRotTxt;
        [SerializeField] protected TMP_Text distanceToObjTxt;
        [SerializeField] protected TMP_Text objPosTxt;
        [FormerlySerializedAs("titlTxt")] [SerializeField] protected TMP_Text tltTxt;
        [SerializeField] protected TMP_Text movedTxt;
        [SerializeField] protected TMP_InputField paramField;
        
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

        /// <summary>
        /// False while the visitor is at another exhibit (VR museum: one provider per object).
        /// An unfocused provider keeps reporting distance so distance-driven effects stay live,
        /// but holds tilt, movement and durations at zero and raises no events. Always true on
        /// the phone, where only one object exists at a time.
        /// </summary>
        public bool IsFocused { get; private set; } = true;

        /// <summary>
        /// What can trace the Object 1 tentacle path: on the phone, the phone itself. The VR
        /// provider adds the hands, because the path loops around the object at podium height.
        /// </summary>
        public virtual Transform[] PathTrackers => _defaultPathTrackers ??= new[] { camTr };

        [HideInInspector] public Transform camTr;
        
        protected float _movementStateStartTime;
        protected Vector3 _camPrevPosition;
        protected float _prevTime;
        protected float _puzzleEnteredYRotation;

        private Transform[] _defaultPathTrackers;
        
        
        protected virtual void Awake()
        {
            camTr = Camera.main.transform;
            _camPrevPosition = camTr.position;
        }
        
        private void Update()
        {
            if (!IsFocused)
            {
                UpdateDistanceToObject();
                return;
            }

            UpdateMovementStatus();
            UpdateDistanceToObject();
            UpdateDeviceTiltAngle();
            UpdateTouchStatus();
            UpdateShakeStatus();
        }

        public virtual void SetPuzzleEnteredRotation()
        {
            _puzzleEnteredYRotation = NormalizeRotationAngles(camTr.rotation.eulerAngles).y;
            UpdateDeviceTiltAngle();
        }

        public void SetFocused(bool isFocused)
        {
            if (IsFocused == isFocused)
            {
                return;
            }

            IsFocused = isFocused;
            TiltZ01 = 0;
            SignedTiltZ01 = 0;
            SignedTiltY01 = 0;
            IsMoving = false;
            MovementDuration = 0;
            IdleDuration = 0;

            // Restart movement tracking so durations count from the moment of focus, not from
            // whatever happened while the visitor was at another exhibit.
            _movementStateStartTime = Time.realtimeSinceStartup;
            _prevTime = Time.realtimeSinceStartup;
            if (camTr)
            {
                _camPrevPosition = camTr.position;
            }

            OnFocusChanged(isFocused);
        }

        protected virtual void OnFocusChanged(bool isFocused)
        {
        }

        
        public bool TryGetParamValue(out float[] val)
        {
            val = new float[] { 0, 0, 0, 0, 0 };

            if (!paramField)
            {
                return false;
            }

            return InteractionUtils.TryParseFloats(paramField.text, val);
        }

        public bool GetForwardRayHit(out RaycastHit hit)
        {
            return Physics.Raycast(camTr.position, camTr.forward, out hit);
        }

        public void SetArObjectTransform(Transform arObjectTransform, MovementPathVisualizer pathVisualizer)
        {
            arObjectTr = arObjectTransform;
            arObjectTr.GetComponentInChildren<ArObjectManagerBase>().Initialize(this, pathVisualizer);
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

            DistanceToArObjectRaw = Vector3.Distance(camTr.position,
                arObjectTr.position);
            
            if (isDebugInfoShown && objPosTxt && distanceToObjTxt)
            {
                objPosTxt.text = "obj pos: " +  arObjectTr.position;
                distanceToObjTxt.text = "Dist to obj: " + DistanceToArObjectRaw;
            }
            else if (isDebugInfoShown)
            {
                // Debug.Log("obj pos: " +  arObjectTr.position);
                Debug.Log("Dist to obj: " + DistanceToArObjectRaw);
            }
            
            DistanceToArObject01 = 1 - Mathf.InverseLerp(minDistance, maxDistance, DistanceToArObjectRaw);
        }
        
        protected static Vector3 NormalizeRotationAngles(Vector3 rotation)
        {
            return InteractionUtils.NormalizeRotationAngles(rotation);
        }
        
        protected abstract void UpdateDeviceTiltAngle();
        protected abstract void UpdateTouchStatus();
        protected abstract void UpdateShakeStatus();
    }
}