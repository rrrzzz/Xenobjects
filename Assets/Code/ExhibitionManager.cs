using System.Collections.Generic;
using Code.Effects;
using UnityEngine;

namespace Code
{
    public class ExhibitionManager : MonoBehaviour
    {
        public float dissolvingDuration = 2;
        public float idleDurationThreshold = 3;
        public float oscillationSpeed = 1;
        public MovementInteractionProviderBase dataProvider;
        
        private OrbGlowingEffect _orbGlow = new OrbGlowingEffect();
        private DistortionEffect _distortion = new DistortionEffect();
        private DissolveEffect _dissolveEffect = new DissolveEffect();
        private List<EffectBase> _effects;
        private bool _isTouchToggleOn;
        private float _idleT;
        private float _movingT;
    
        private void Start()
        {
            _effects = new List<EffectBase>{_orbGlow, _distortion, _dissolveEffect};
            dataProvider.DoubleTouchEvent.AddListener(OnDoubleTouch);
            dataProvider.ArObjectSetEvent.AddListener(SetMaterials);
        }
        
        private void Update()
        {
            _orbGlow.SetEffectByNormalizedValue(dataProvider.DistanceToArObject01);   
            _distortion.SetEffectByNormalizedValue(dataProvider.Tilt01);
            
            if (_isTouchToggleOn)
            {
                _distortion.SetOscillatingEffect(oscillationSpeed);
            }

            SetMovementChangingDissolution();
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
                    _idleT = Mathf.InverseLerp(0, dataProvider.IdleDuration + elapsedDissolvingTime, 
                        dissolvingDuration);
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
            _isTouchToggleOn = !_isTouchToggleOn;
            _distortion.ToggleOscillatingEffect();
        }

        private void OnDestroy()
        {
            dataProvider.DoubleTouchEvent.RemoveListener(OnDoubleTouch);
            dataProvider.ArObjectSetEvent.RemoveListener(SetMaterials);
        }
    }
}