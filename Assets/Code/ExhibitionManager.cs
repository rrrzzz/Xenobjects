using System.Collections.Generic;
using Code.Effects;
using SplineMesh;
using UnityEngine;

namespace Code
{
    public class ExhibitionManager : MonoBehaviour
    {
        public float dissolvingDuration = 2;
        public float idleDurationThreshold = 3;
        public float oscillationSpeed = 1;
        public MovementInteractionProviderBase dataProvider;

        private SplineMeshTiling _splineMeshTiling;
        private ExampleTentacle _exampleTentacle;
        private OrbGlowingEffect _orbGlow = new OrbGlowingEffect();
        private DistortionEffect _distortion = new DistortionEffect();
        private DissolveEffect _dissolveEffect = new DissolveEffect();
        private List<EffectBase> _effects;
        private bool _isTouchToggleOn;
        private float _idleT;
        private float _movingT;
    
        private void Start()
        {
            _effects = new List<EffectBase>{_orbGlow, _distortion};
            dataProvider.DoubleTouchEvent.AddListener(OnDoubleTouch);
            dataProvider.ArObjectSetEvent.AddListener(SetMaterials);
            dataProvider.ArObjectSetEvent.AddListener(SetTentacleComponents);
            dataProvider.ShakeEvent.AddListener(ResetScaleAndTime);
        }

        private void ResetScaleAndTime()
        {
            _splineMeshTiling.resetScalingAndTime = true;
            // _exampleTentacle.startScale += 0.001f;
            // _exampleTentacle.ReapplyScaleAndRoll();
        }

        private void SetTentacleComponents()
        {
            _exampleTentacle = dataProvider.arObjectTr.GetComponentInChildren<ExampleTentacle>();
            _splineMeshTiling = dataProvider.arObjectTr.GetComponentInChildren<SplineMeshTiling>();
        }

        private void Update()
        {
            _orbGlow.SetEffectByNormalizedValue(dataProvider.DistanceToArObject01);   
            _distortion.SetEffectByNormalizedValue(dataProvider.Tilt01);
            
            if (_isTouchToggleOn)
            {
                _distortion.SetOscillatingEffect(oscillationSpeed);
            }

            // SetMovementChangingDissolution();
        }

        private void SetMaterials()
        {
            _effects.ForEach(x => x.SetMaterial(dataProvider.arObjectTr));
        }

        private void SetMovementChangingDissolution()
        {
            if (!dataProvider.IsMoving)
            {
                _idleT = 0;
                if (_movingT > 0)
                {
                    var elapsedDissolvingTime = dissolvingDuration * _movingT;

                    _idleT = Mathf.InverseLerp(0, dissolvingDuration, 
                        dataProvider.IdleDuration + elapsedDissolvingTime);
                }
                else if (dataProvider.IdleDuration > idleDurationThreshold)
                {
                    _idleT = Mathf.InverseLerp(idleDurationThreshold, idleDurationThreshold + dissolvingDuration, 
                        dataProvider.IdleDuration);
                }
                
                _dissolveEffect.SetEffectByNormalizedValue(_idleT);
            }
            else
            {
                var elapsedAssemblyTime = dissolvingDuration * (1 - _idleT);
                
                _movingT = 1 - Mathf.InverseLerp(0, dissolvingDuration, 
                    dataProvider.MovementDuration + elapsedAssemblyTime);

                _dissolveEffect.SetEffectByNormalizedValue(_movingT);
            }
        }

        private void OnDoubleTouch()
        {
            _splineMeshTiling.scaleToFull = !_splineMeshTiling.scaleToFull;
            _isTouchToggleOn = !_isTouchToggleOn;
            _distortion.ToggleOscillatingEffect();
        }

        private void OnDestroy()
        {
            dataProvider.DoubleTouchEvent.RemoveListener(OnDoubleTouch);
            dataProvider.ArObjectSetEvent.RemoveListener(SetMaterials);
            dataProvider.ArObjectSetEvent.RemoveListener(SetTentacleComponents);
        }
    }
}