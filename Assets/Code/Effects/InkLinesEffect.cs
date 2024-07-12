using UnityEngine;

namespace Code.Effects
{
    public class InkLinesEffect : EffectBase
    {
        public InkLinesEffect()
        {
            MaterialName = "smoke trail alpha";
            ShaderID_1 = Shader.PropertyToID("_TintColor");
        }
    }
}