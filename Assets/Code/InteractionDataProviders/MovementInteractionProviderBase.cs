using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
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
        [SerializeField] protected TMP_Text titlTxt;
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

        [HideInInspector] public Transform camTr;
        
        protected float _movementStateStartTime;
        protected Vector3 _camPrevPosition;
        protected float _prevTime;
        protected float _puzzleEnteredYRotation;
        
        
        protected virtual void Awake()
        {
            camTr = Camera.main.transform;
            _camPrevPosition = camTr.position;
        }
        
        private void Update()
        { 
            UpdateMovementStatus();
            UpdateDistanceToObject();
            UpdatePhoneTiltAngle();
            UpdateTouchStatus();
            UpdateShakeStatus();
        }
        
        public void SetPuzzleEnteredRotation()
        {
            _puzzleEnteredYRotation = NormalizeRotationAngles(camTr.rotation.eulerAngles).y;
            UpdatePhoneTiltAngle();
        }

        
        public bool TryGetParamValue(out float[] val)
        {
            val = new float[]{0,0,0,0,0};

            if (paramField)
            {
                // Split the string by commas
                string[] parts = paramField.text.Split(',');

                for (int i = 0; i < parts.Length; i++)
                {
                    val[i] = float.Parse(parts[i], CultureInfo.InvariantCulture);
                }
                
                return true;
            }
        
            return false;
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
            
            if (!(Time.realtimeSinceStartup - _prevTime > movementTrackingInterval)) return;
            
            var movementDist = Vector3.Distance(camTr.position, _camPrevPosition);
            _camPrevPosition = camTr.position;
            _prevTime = Time.realtimeSinceStartup;
            
            var currentMovementStatus = movementDist > movementThreshold;
            if (currentMovementStatus == IsMoving) return;
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
            if (!arObjectTr) return;

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
            if (rotation.x > 180)
                rotation.x -= 360;

            if (rotation.y > 180)
                rotation.y -= 360;
            
            if (rotation.z > 180)
                rotation.z -= 360;
            
            return rotation * -1;
        }
        
        protected abstract void UpdatePhoneTiltAngle();
        protected abstract void UpdateTouchStatus();
        protected abstract void UpdateShakeStatus();
    }
}