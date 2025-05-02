using UnityEngine;

namespace Code
{
    public class MovementInteractionTestProvider : MovementInteractionProviderBase
    {
        public float camRotationInputSpeed = 40;
        public bool addTransformOnAwake;
        public MovementPathVisualizer movementPathVisualizer;

        protected override void Awake()
        {
            base.Awake();
            if (!addTransformOnAwake)
            {
                return;
            }

            if (!movementPathVisualizer)
            {
                Debug.LogError("No path viz added to MovementInteractionTestProvider.");
                return;
            }
            
            SetArObjectTransform(arObjectTr, movementPathVisualizer);
        }

        protected override void UpdatePhoneTiltAngle()
        {
            var camRot = camTr.rotation.eulerAngles;
            if (Input.GetKey(KeyCode.Q))
            {
                camRot.z += camRotationInputSpeed * Time.deltaTime;
            }

            if (Input.GetKey(KeyCode.E))
            {
                camRot.z -= camRotationInputSpeed * Time.deltaTime;
            }
            
            if (Input.GetKey(KeyCode.Z))
            {
                camRot.y += camRotationInputSpeed * Time.deltaTime;
            }

            if (Input.GetKey(KeyCode.C))
            {
                camRot.y -= camRotationInputSpeed * Time.deltaTime;
            }

            var rotNormalized = NormalizeRotationAngles(camRot);
            var correctedRotY = rotNormalized.y - _puzzleEnteredYRotation;
            var rotYAbs = Mathf.Abs(correctedRotY);
            var rotZAbs = Mathf.Abs(rotNormalized.z);
            if (rotYAbs < maxTiltY || rotZAbs < maxTilt)
            {
                camTr.rotation = Quaternion.Euler(camRot);
            }

            SignedTiltY01 = Mathf.Clamp(correctedRotY, -maxTiltY, maxTiltY) / maxTiltY;
            TiltZ01 = Mathf.InverseLerp(0, maxTilt, rotZAbs);
            SignedTiltZ01 = rotNormalized.z < 0 ? TiltZ01 * -1 : TiltZ01;
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
            if (Input.GetKeyDown(KeyCode.S))
            {
                ShakeEvent.Invoke();
            }
        }
    }
}