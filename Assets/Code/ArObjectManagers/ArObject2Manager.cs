using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

namespace Code
{
    public class ArObject2Manager : ArObjectManagerBase
    {
        public float startDelay = 4;
        public float idleDurationMax = 4;
        public float movingDurationMax = 4;   
        public float tornadoMinDistance = .7f;
        public float tornadoMaxDistance = 3f;
        public ParticleSystem tornadoPs;
        public float tornadoFadeDelay = 12;
        public Transform bloodSystemA;
        public Transform bloodSystemC;
        public float bloodRotationAngles = 100;

        public Transform tornadoTransform;
        public ParticleSystem gasPs;
        
        private bool _delayPassed;
        private float _startTime;
        
        private Vector3 _tornadoInitPos = new Vector3 (-10.443f, 18.035f, -0.203f);
        private Vector3 _tornadoFinalPos = new Vector3 (1.2f, -11.400f, -0.203f);
        private Vector3 _tornadoInitScale = new Vector3(1.466175f, 1.466175f, 1.466175f);
        private Vector3 _tornadoFinalScale = new Vector3(16.91379f, 16.91379f, 16.91379f);
        
        private Material _gasMat;
        private Material _tornadoMat;
        private static readonly int GasColorId = Shader.PropertyToID("_Color");
        private static readonly int TintColorId = Shader.PropertyToID("_TintColor");

        private readonly Dictionary<string, Vector2> _psColorValues = new Dictionary<string, Vector2>
        {
            { "Fly Particles", new Vector2(128, 0) },
            { "Flames Secondary", new Vector2(20, 0) },
            { "Flames Big", new Vector2(10, 0) },
            { "Ashes", new Vector2(20, 0) },
            { "Burning Smoke", new Vector2(20, 100) },
            { "Burning Dark", new Vector2(61, 170) },
            { "Smoke Column", new Vector2(50, 170) },
            { "Heat Distortion", new Vector2(80, 0) },
            { "Particle System_A", new Vector2(70, 0) },
            { "Particle System_C", new Vector2(70, 0) }
        };
        
        private readonly Vector3 _bloodAInitRot = new Vector3(-9.379f, 56.28f, -240.005f);
        private readonly Vector3 _bloodCInitRot = new Vector3(-60.69f, 422.773f, 124.218f);
        private Tweener _bloodTweenA;
        private Tweener _bloodTweenC;
         
        private ParticleSystem[] _particleSystems;
        private bool _isChangingAlpha = true;
        public bool isBloodRotating;

        public override void Initialize(MovementInteractionProviderBase dataProvider)
        {
            base.Initialize(dataProvider);
            DataProvider.SingleTouchEvent.AddListener(OnSingleTouch);
            _startTime = Time.realtimeSinceStartup;
            _gasMat = gasPs.GetComponent<Renderer>().material;
            _tornadoMat = tornadoPs.GetComponent<Renderer>().material;
            _particleSystems = transform.parent.GetComponentsInChildren<ParticleSystem>();
        }

        private void OnSingleTouch()
        {
            isBloodRotating = !isBloodRotating;

            if (!isBloodRotating)
            {
                _bloodTweenA = bloodSystemA.DOLocalRotate(_bloodAInitRot, 1);
                _bloodTweenC = bloodSystemC.DOLocalRotate(_bloodCInitRot, 1);
                return;
            }

            if (_bloodTweenA != null)
            {
                _bloodTweenA.Kill();
                _bloodTweenC.Kill();
            }
        }

        private void Update()
        {
            if (!IsInitialized)
            {
                return;
            }
            
            var timePassed = Time.realtimeSinceStartup - _startTime;
            if (!_delayPassed && timePassed < startDelay)
            {
                return;
            }

            _delayPassed = true;

            if (isBloodRotating)
            {
                bloodSystemA.Rotate(Vector3.up, bloodRotationAngles * Time.deltaTime, Space.World);
                bloodSystemC.Rotate(Vector3.up, -bloodRotationAngles * Time.deltaTime, Space.World);
            }
            
            if (timePassed > tornadoFadeDelay)
            {
                var tornadoFadingTime = timePassed - tornadoFadeDelay; 
                if (tornadoFadingTime <= 2)
                {
                    var t = tornadoFadingTime / 2;
                    var alpha = Mathf.Lerp(1, 0.04f, t);
                    var currentCol = _tornadoMat.GetColor(TintColorId);
                    currentCol.a = alpha;
                    _tornadoMat.SetColor(TintColorId, currentCol);
                }
            }

            UpdateTornadoTransform();
            if (!_isChangingAlpha)
            {
                return;
            }
            UpdateParticleSystemsVisibilityByMovement();
        }

        private void UpdateTornadoTransform()
        {
            var t = Mathf.InverseLerp(tornadoMinDistance, tornadoMaxDistance, DataProvider.DistanceToArObjectRaw);
            if (t > 0.2f) // distance to object is large enough
            {
                _isChangingAlpha = false;
                UpdateAlphaColorParticleSystems(1, true);
            }
            else
            {
                _isChangingAlpha = true;
            }
            
            tornadoTransform.localScale = Vector3.Lerp(_tornadoInitScale, _tornadoFinalScale, t);
            tornadoTransform.localPosition = Vector3.Lerp(_tornadoInitPos, _tornadoFinalPos, t);
        }

        private void UpdateParticleSystemsVisibilityByMovement()
        {
            // if (DataProvider.IsMoving && DataProvider.MovementDuration > movingDurationMax)
            // {
            //     return;
            // }
            //
            // if (!DataProvider.IsMoving && DataProvider.IdleDuration > idleDurationMax)
            // {
            //     return;
            // }
            //
            
            var t = DataProvider.IsMoving ? Mathf.InverseLerp(0, movingDurationMax, DataProvider.MovementDuration) : 
                Mathf.InverseLerp(0, idleDurationMax, DataProvider.IdleDuration);
            
            UpdateAlphaColorParticleSystems(t, DataProvider.IsMoving);
            UpdateMaterialAlphaParticleSystems(t, DataProvider.IsMoving);
        }

        private void UpdateAlphaColorParticleSystems(float t, bool isMoving)
        {
            foreach (var ps in _particleSystems)
            {
                if (!_psColorValues.TryGetValue(ps.name, out var colorValue))
                {
                    continue;
                }

                var max = colorValue.y == 0 ? 255 : colorValue.y;
                var alpha = Mathf.Lerp(colorValue.x, isMoving ? 0 : max, t);
                var main = ps.main;
                var startColor = main.startColor.color;
                startColor.a = alpha;
                main.startColor = startColor;
            }
        }

        private void UpdateMaterialAlphaParticleSystems(float t, bool isMoving)
        {
            // var tornadoAlpha = isMoving ? Mathf.Lerp(TornadoStart, TornadoMin, t) : Mathf.Lerp(TornadoStart, TornadoMax, t);
            //
            // _tornadoMat.SetColor(TornadoTint, new Vector4(128 / 255f, 128 / 255f, 128 / 255f, tornadoAlpha / 255f));
            
            var gasAlpha = isMoving ? Mathf.Lerp(75, 0, t) : Mathf.Lerp(75, 255, t);
            _gasMat.SetColor(GasColorId, new Vector4(154 / 255f, 154 / 255f, 154 / 255f, gasAlpha / 255f));
        }
    }
}