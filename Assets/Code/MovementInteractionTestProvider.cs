using UnityEngine;

namespace Code
{
    public class MovementInteractionTestProvider : MovementInteractionProviderBase
    {
        public float zRotationInputSpeed = 1;
        
        protected override void UpdatePhoneTiltAngle()
        {
            var camRot = _camTr.rotation.eulerAngles;
            if (Input.GetKey(KeyCode.Q))
            {
                camRot.z += zRotationInputSpeed * Time.deltaTime;
            }

            if (Input.GetKey(KeyCode.E))
            {
                camRot.z -= zRotationInputSpeed * Time.deltaTime;
            }

            var rotNormalized = NormalizeAngle(camRot.z);
            rotNormalized = Mathf.Abs(rotNormalized);
            if (rotNormalized < maxTilt)
            {
                _camTr.rotation = Quaternion.Euler(camRot);
            }
            Debug.Log(":noerm: " + rotNormalized);

            Tilt01 = Mathf.InverseLerp(0, maxTilt, rotNormalized); 
            Debug.Log(Tilt01);
        }
        
        protected override void UpdateTouchStatus()
        {
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