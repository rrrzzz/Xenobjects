using System.Diagnostics;
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
            MaterialName = "Water_MeshEffect";
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
            Material.SetFloat(ShaderID_2, val);
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