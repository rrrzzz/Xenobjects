using UnityEngine;

namespace Code
{
    public class ARMovementInteractionDataProvider : MovementInteractionProviderBase
    {
        private const float ShakeDetectionThreshold = 2.0f;
        private const float TouchTimeThreshold = 0.2f;
        private const float LowPassFilterFactor = 0.0166f;
        
        private Vector3 _lowPassValue;
        
        protected override void Start()
        {
            base.Start();
            _lowPassValue = Input.acceleration;
        }
        
        protected override void UpdatePhoneTiltAngle()
        {
            var camRot = _camTr.rotation.eulerAngles;
            cameraPosRotTxt.text = $"Phone pos: {_camTr.position}\nPhone rot: {camRot}\n";
            
            var rotNormalized = NormalizeAngle(camRot.z) * -1;
            rotNormalized = Mathf.Abs(rotNormalized);
            Tilt01 = Mathf.InverseLerp(0, maxTilt, rotNormalized); 
            titlTxt.text = Mathf.Abs(rotNormalized) + "Tilt 01: " + Tilt01;
        }
        
        protected override void UpdateTouchStatus()
        {
            if (Input.touchCount != 2) return;
            
            var touch1 = Input.GetTouch(0);
            var touch2 = Input.GetTouch(1);

            if (touch1.phase != TouchPhase.Began || touch2.phase != TouchPhase.Began) return;
            
            var timeDifference = Mathf.Abs(touch1.deltaTime - touch2.deltaTime);

            if (!(timeDifference < TouchTimeThreshold)) return;
            
            titlTxt.text += " Touch at: " + Time.time;
            
            DoubleTouchEvent.Invoke();
        }

        protected override void UpdateShakeStatus()
        {
            var acceleration = Input.acceleration;
            _lowPassValue = Vector3.Lerp(_lowPassValue, acceleration, LowPassFilterFactor);
            var deltaAcceleration = acceleration - _lowPassValue;

            if (deltaAcceleration.sqrMagnitude >= ShakeDetectionThreshold)
            {
                ShakeEvent.Invoke();
                shakeText.text = "Shake event detected at time " + Time.time;
            }
        }
        
        // private void UpdateTouchStatusDebug()
        // {
        //     if (Input.touchCount == 2)
        //     {
        //         var touch1 = Input.GetTouch(0);
        //         var touch2 = Input.GetTouch(1);
        //
        //         if (touch1.phase == TouchPhase.Began && touch2.phase == TouchPhase.Began)
        //         {
        //             float timeDifference = Mathf.Abs(touch1.deltaTime - touch2.deltaTime);
        //
        //             if (timeDifference < TouchTimeThreshold)
        //             {
        //                 isDoubleTouchDetected = true;
        //                 // Two-finger touch detected
        //                 titlTxt.text = "Two-finger touch detected!";
        //             }
        //         }
        //     }
        //     else if (Input.touchCount == 1)
        //     {
        //         titlTxt.text = "Two-finger touch NOT detected!";
        //     }
        // }
    }
}