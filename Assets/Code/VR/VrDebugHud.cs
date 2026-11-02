using TMPro;
using UnityEngine;

namespace Code.VR
{
    /// <summary>
    /// Wrist-mounted readout of the focused exhibit's provider: which exhibit has focus, head
    /// distance, head roll, puzzle yaw, movement state, the last tap/shake and the live shake
    /// signal. It observes the providers instead of having them write UI.
    /// </summary>
    public class VrDebugHud : MonoBehaviour
    {
        [SerializeField] private VrExhibitFocus focus;
        [SerializeField] private TMP_Text readout;
        [SerializeField] private float refreshInterval = 0.2f;

        private float _nextRefreshTime;
        private float _lastSingleTouchTime = float.NegativeInfinity;
        private float _lastDoubleTouchTime = float.NegativeInfinity;
        private float _lastShakeTime = float.NegativeInfinity;

        private void Start()
        {
            if (!focus)
                return;

            for (var i = 0; i < focus.ExhibitCount; i++)
            {
                var provider = focus.GetExhibit(i).provider;
                provider.SingleTouchEvent.AddListener(OnSingleTouch);
                provider.DoubleTouchEvent.AddListener(OnDoubleTouch);
                provider.ShakeEvent.AddListener(OnShake);
            }
        }

        private void OnDestroy()
        {
            if (!focus)
                return;

            for (var i = 0; i < focus.ExhibitCount; i++)
            {
                var provider = focus.GetExhibit(i).provider;
                if (!provider)
                    continue;

                provider.SingleTouchEvent.RemoveListener(OnSingleTouch);
                provider.DoubleTouchEvent.RemoveListener(OnDoubleTouch);
                provider.ShakeEvent.RemoveListener(OnShake);
            }
        }

        private void OnSingleTouch()
        {
            _lastSingleTouchTime = Time.time;
        }

        private void OnDoubleTouch()
        {
            _lastDoubleTouchTime = Time.time;
        }

        private void OnShake()
        {
            _lastShakeTime = Time.time;
        }

        private void Update()
        {
            if (!focus || !readout || Time.unscaledTime < _nextRefreshTime)
                return;

            _nextRefreshTime = Time.unscaledTime + refreshInterval;
            readout.text = BuildReadout();
        }

        private string BuildReadout()
        {
            var events = $"Tap {Ago(_lastSingleTouchTime)}   Double {Ago(_lastDoubleTouchTime)}   Shake {Ago(_lastShakeTime)}";
            var exhibit = focus.FocusedExhibit;
            if (exhibit == null)
                return "No exhibit in reach - walk up to a podium\n" + events;

            var provider = exhibit.provider;
            var movement = provider.IsMoving
                ? $"MOVING {provider.MovementDuration:F1} s"
                : $"IDLE {provider.IdleDuration:F1} s";
            var text = $"<b>{exhibit.label}</b>   head {provider.DistanceToArObjectRaw:F2} m   near01 {provider.DistanceToArObject01:F2}\n" +
                       $"Head roll  TiltZ01 {provider.TiltZ01:F2}  (signed {provider.SignedTiltZ01:F2})\n" +
                       $"Puzzle yaw  SignedTiltY01 {provider.SignedTiltY01:F2}\n" +
                       $"{movement}\n{events}";

            if (provider is VRMovementInteractionDataProvider vrProvider)
            {
                var shake = vrProvider.Shake;
                var source = vrProvider.IsUsingDeviceVelocity ? "XR velocity" : "position fallback";
                text += $"\nShake dSq L {shake.LeftDeltaSq:F1}  R {shake.RightDeltaSq:F1}   " +
                        $"bursts L {shake.LeftBurstCount}  R {shake.RightBurstCount}   ({source})";
            }

            return text;
        }

        private static string Ago(float time)
        {
            return float.IsNegativeInfinity(time) ? "-" : $"{Time.time - time:F1}s ago";
        }
    }
}
