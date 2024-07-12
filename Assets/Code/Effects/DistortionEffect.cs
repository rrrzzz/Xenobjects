using System.Diagnostics;
using DG.Tweening;
using UnityEngine;

namespace Code.Effects
{
    public class DistortionEffect : EffectBase
    {
        private const float DistortionMin = 0;
        private const float DistortionMax = 150;
        
        private Stopwatch _oscillationTimer = new Stopwatch();
        private bool _isOscillating;
        
        public DistortionEffect()
        {
            ShaderID_1 = Shader.PropertyToID("_Distortion");
            ShaderID_2 = Shader.PropertyToID("_RefractiveStrength");
            IsUsingFloat = true;
            MinFloat = DistortionMin;
            MaxFloat = DistortionMax;
        }
        
        public void SetOscillatingEffect(float oscillationSpeed)
        {
            var t = _oscillationTimer.ElapsedMilliseconds / 1000f * oscillationSpeed;

            var val = Mathf.Sin(t);
            foreach (var material in Materials)
            {
                material.SetFloat(ShaderID_2, val);
            }
        }

        public void FadeEffects(float fadeTime)
        {
            foreach (var material in Materials)
            {
                material.DOFloat(0, ShaderID_1, fadeTime);
                material.DOFloat(0, ShaderID_2, fadeTime);
            }
        }

        public void ToggleOscillatingEffect()
        {
            _isOscillating = !_isOscillating;
            if (_isOscillating)
            {
                _oscillationTimer.Start();
            }
            else
            {
                _oscillationTimer.Stop();
            }
        }
    }
}