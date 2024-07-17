using UnityEngine;

namespace Code
{
    public class MovementInteractionTestProvider : MovementInteractionProviderBase
    {
        public float camRotationInputSpeed = 40;

        protected override void Awake()
        {
            base.Awake();
            SetArObject2Transform(arObjectTr);
        }

        protected override void UpdatePhoneTiltAngle()
        {
            var camRot = _camTr.rotation.eulerAngles;
            if (Input.GetKey(KeyCode.Q))
            {
                camRot.z += camRotationInputSpeed * Time.deltaTime;
            }

            if (Input.GetKey(KeyCode.E))
            {
                camRot.z -= camRotationInputSpeed * Time.deltaTime;
            }

            var rotNormalized = NormalizeAngle(camRot.z);
            rotNormalized = Mathf.Abs(rotNormalized);
            if (rotNormalized < maxTilt)
            {
                _camTr.rotation = Quaternion.Euler(camRot);
            }

            TiltZ01 = Mathf.InverseLerp(0, maxTilt, rotNormalized); 
            
            if (Input.GetKey(KeyCode.Z))
            {
                camRot.x += camRotationInputSpeed * Time.deltaTime;
            }

            if (Input.GetKey(KeyCode.C))
            {
                camRot.x -= camRotationInputSpeed * Time.deltaTime;
            }
            
            rotNormalized = NormalizeAngle(camRot.x);
            rotNormalized = Mathf.Abs(rotNormalized);
            if (rotNormalized < maxTilt)
            {
                _camTr.rotation = Quaternion.Euler(camRot);
            }
            
            TiltX01 = Mathf.InverseLerp(0, maxTilt, rotNormalized); 
        }
        
        protected override void UpdateTouchStatus()
        {
            if (Input.GetKeyDown(KeyCode.U))
            {
                SingleTouchEvent.Invoke();
            }
            if (Input.GetKeyDown(KeyCode.T))
            {
                DoubleTouchEvent.Invoke();
            }
        }
        
        protected override void UpdateShakeStatus()
        {
            if (Input.GetKeyDown(KeyCode.Space))
            {
                ShakeEvent.Invoke();
            }
        }
        
        
    }
}