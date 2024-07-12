using UnityEngine;

namespace Code.Effects
{
    public class DissolveEffect : EffectBase
    {
        private const float DissolveMax = 1;
        private const float DissolveMin = 0;
        
        public DissolveEffect()
        {
            ShaderID_1 = Shader.PropertyToID("_cutoff");
            IsUsingFloat = true;
            MinFloat = DissolveMin;
            MaxFloat = DissolveMax;
        }
    }
}