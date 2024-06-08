using TMPro;
using UnityEngine;
using UnityEngine.Events;

namespace Code
{
    public abstract class MovementInteractionProviderBase : MonoBehaviour
    {
        public Transform arObjectTr;
        [SerializeField] protected bool isDebugInfoShown;
        [SerializeField] protected float maxTilt = 60;
        [SerializeField] protected float maxDistance = 3;
        [SerializeField] protected float minDistance = 1.5f;
        [SerializeField] protected float movementTrackingInterval = 1f;
        [SerializeField] protected float movementThreshold = 0.2f;
        
        [SerializeField] protected TMP_Text shakeText;
        [SerializeField] protected TMP_Text cameraPosRotTxt;
        [SerializeField] protected TMP_Text distanceToObjTxt;
        [SerializeField] protected TMP_Text objPosTxt;
        [SerializeField] protected TMP_Text titlTxt;
        [SerializeField] protected TMP_Text movedTxt;
        
        public float DistanceToArObject01 { get; set; }
        public float Tilt01 { get; set; }
        public bool IsMoving { get; set; }
        public float MovementDuration { get; set; }
        public float IdleDuration { get; set; }
        public UnityEvent DoubleTouchEvent { get; } = new UnityEvent();
        public UnityEvent ShakeEvent { get; } = new UnityEvent();
        public UnityEvent ArObjectSetEvent { get; } = new UnityEvent();
        
        protected float _movementStateStartTime;
        protected Transform _camTr;
        protected Vector3 _camPrevPosition;
        protected float _prevTime;
        
        protected virtual void Start()
        {
            _camTr = Camera.main.transform;
            _camPrevPosition = _camTr.position;
        }
        
        private void Update()
        { 
            UpdateMovementStatus();
            UpdateDistanceToObject();
            UpdatePhoneTiltAngle();
            UpdateTouchStatus();
            UpdateShakeStatus();
        }
        
        public void SetArObjectTransform(Transform arObjectTransform)
        {
            arObjectTr = arObjectTransform;
            ArObjectSetEvent.Invoke();
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
            
            if (!(Time.realtimeSinceStartup - _prevTime > movementTrackingInterval)) return;
            
            var movementDist = Vector3.Distance(_camTr.position, _camPrevPosition);
            _camPrevPosition = _camTr.position;
            _prevTime = Time.realtimeSinceStartup;
            
            var currentMovementStatus = movementDist > movementThreshold;
            if (currentMovementStatus == IsMoving) return;
            IsMoving = currentMovementStatus;
            Debug.Log(IsMoving ? "MOVING" : "IDLE");
            
            if (isDebugInfoShown && movedTxt)
            {
                movedTxt.text = IsMoving ? "MOVING" : "IDLE";
            }
            if (!IsMoving)
            {
                MovementDuration = 0;
            }
            else
            {
                IdleDuration = 0;
            }
            
            _movementStateStartTime = Time.realtimeSinceStartup;
        }
        
        private void UpdateDistanceToObject()
        {
            if (!arObjectTr) return;

            var distance = Vector3.Distance(_camTr.position,
                arObjectTr.position);
            
            if (isDebugInfoShown && objPosTxt && distanceToObjTxt)
            {
                objPosTxt.text = "obj pos: " +  arObjectTr.position;
                distanceToObjTxt.text = "Dist to obj: " + distance;
            }
            
            DistanceToArObject01 = 1 - Mathf.InverseLerp(minDistance, maxDistance, distance);
        }
        
        protected static float NormalizeAngle(float angle)
        {
            if (angle > 180)
                angle -= 360;
            return angle;
        }
        
        protected abstract void UpdatePhoneTiltAngle();
        protected abstract void UpdateTouchStatus();
        protected abstract void UpdateShakeStatus();
    }
}