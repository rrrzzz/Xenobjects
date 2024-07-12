using UnityEngine;

namespace Code.Effects
{
    public class RisingSteamEffect : EffectBase
    {
        public RisingSteamEffect()
        {
            ShaderID_1 = Shader.PropertyToID("_Color");
        }
    }
}