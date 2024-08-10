using System.Collections.Generic;
using UnityEngine;

namespace Code
{
    public class ArObject1Manager : ArObjectManagerBase
    {
        public const float TornadoMin = 0;
        public const float TornadoStart = 10f;
        public const float TornadoMax = 255f;
        
        public float startDelay = 4;
        public float distanceMin = 0.7f;
        public float distanceMax = 2.5f;
        public float idleDurationMax = 4;
        public float movingDurationMax = 4; 
        
        public ParticleSystem tornadoPs;
        public ParticleSystem gasPs;
        
        private bool _delayPassed;
        private float _startTime;
        
        private Material _tornadoMat;
        private static readonly int TornadoTint = Shader.PropertyToID("_TintColor");
        private Material _gasMat;
        private static readonly int GasColorId = Shader.PropertyToID("_Color");

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

        private ParticleSystem[] _particleSystems;
        
        public override void Initialize(MovementInteractionProviderBase dataProvider)
        {
            base.Initialize(dataProvider);
            _startTime = Time.realtimeSinceStartup;
            _tornadoMat = tornadoPs.GetComponent<Renderer>().material;
            _gasMat = gasPs.GetComponent<Renderer>().material;
            _particleSystems = transform.parent.GetComponentsInChildren<ParticleSystem>();
        }

        private void Update()
        {
            if (!IsInitialized)
            {
                return;
            }
            
            if (!_delayPassed && Time.realtimeSinceStartup - _startTime < startDelay)
            {
                return;
            }

            _delayPassed = true;

            UpdateParticleSystemsVisibilityByMovement();
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

            UpdateColorParticleSystems(t, DataProvider.IsMoving);
            UpdateMaterialParticleSystems(t, DataProvider.IsMoving);
        }

        private void UpdateColorParticleSystems(float t, bool isMoving)
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

        private void UpdateMaterialParticleSystems(float t, bool isMoving)
        {
            var tornadoAlpha = isMoving ? Mathf.Lerp(TornadoStart, TornadoMin, t) : Mathf.Lerp(TornadoStart, TornadoMax, t);

            _tornadoMat.SetColor(TornadoTint, new Vector4(128 / 255f, 128 / 255f, 128 / 255f, tornadoAlpha / 255f));
            
            var gasAlpha = isMoving ? Mathf.Lerp(75, 0, t) : Mathf.Lerp(75, 255, t);
            _gasMat.SetColor(GasColorId, new Vector4(154 / 255f, 154 / 255f, 154 / 255f, gasAlpha / 255f));
        }
    }
}