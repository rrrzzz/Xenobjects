using UnityEngine;

namespace Code
{
    public class ARMovementInteractionDataProvider : MovementInteractionProviderBase
    {
        private const float ShakeDetectionThreshold = 2.0f;
        private const float TouchTimeThreshold = 0.2f;
        // Equivalent to the old per-frame lerp factor of 0.0166 at 60 fps,
        // but framerate-independent (see UpdateShakeStatus).
        private const float LowPassTimeConstant = 1f;
        private const float ShakeCooldown = 0.35f;

        private Vector3 _lowPassValue;
        private float _lastShakeTime = float.NegativeInfinity;

        protected override void Awake()
        {
            base.Awake();
            _lowPassValue = Input.acceleration;
        }
        
        protected override void UpdateDeviceTiltAngle()
        {
            var camRot = camTr.rotation.eulerAngles;
            
            var rotNormalized = NormalizeRotationAngles(camRot);
            var correctedRotY = rotNormalized.y - _puzzleEnteredYRotation;

            if (isDebugInfoShown && cameraPosRotTxt)
            {
                cameraPosRotTxt.text = $"Phone pos: {camTr.position}\nPhone rot: {rotNormalized}\n";
            }
            
            SignedTiltY01 = Mathf.Clamp(correctedRotY, -maxTiltY, maxTiltY) / maxTiltY;
            SignedTiltZ01 = Mathf.Clamp(rotNormalized.z, -maxTilt, maxTilt) / maxTilt;
            
            // cameraPosRotTxt.text = $"Phone pos: {_camTr.position}\nSignedZ: {SignedTiltZ01} SignedTiltY01: {SignedTiltY01}\n";
            TiltZ01 = Mathf.Abs(SignedTiltZ01);
        }
        
        protected override void UpdateTouchStatus()
        {
            if (Input.touchCount == 1)
            {
                var touch = Input.GetTouch(0);
                if (touch.phase != TouchPhase.Began) 
                    return;
                
                if (isDebugInfoShown && tltTxt)
                {
                    tltTxt.text = "Single Touch at: " + Time.time;
                }
                SingleTouchEvent.Invoke();
                return;
            }
            
            if (Input.touchCount != 2) return;
            
            var touch1 = Input.GetTouch(0);
            var touch2 = Input.GetTouch(1);

            if (touch1.phase != TouchPhase.Began || touch2.phase != TouchPhase.Began) return;
            
            var timeDifference = Mathf.Abs(touch1.deltaTime - touch2.deltaTime);

            if (!(timeDifference < TouchTimeThreshold)) return;

            if (isDebugInfoShown && tltTxt)
            {
                tltTxt.text = "Double Touch at: " + Time.time;
            }
            
            DoubleTouchEvent.Invoke();
        }

        protected override void UpdateShakeStatus()
        {
            var acceleration = Input.acceleration;
            var alpha = 1f - Mathf.Exp(-Time.deltaTime / LowPassTimeConstant);
            _lowPassValue = Vector3.Lerp(_lowPassValue, acceleration, alpha);
            var deltaAcceleration = acceleration - _lowPassValue;

            if (deltaAcceleration.sqrMagnitude < ShakeDetectionThreshold)
            {
                return;
            }

            if (Time.time - _lastShakeTime < ShakeCooldown)
            {
                return;
            }

            _lastShakeTime = Time.time;
            ShakeEvent.Invoke();

            if (isDebugInfoShown && shakeText)
            {
                shakeText.text = "Shake event detected at time " + Time.time;
            }
        }
    }
}