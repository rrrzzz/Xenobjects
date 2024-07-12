using DG.Tweening;
using UnityEngine;

namespace Code.Effects
{
    public class OrbGlowingEffect : EffectBase
    {
        private const float XMax = 15;
        private const float WMax = 15;
        private const float OriginalGradient = .5f;
        private const float FadedGradient = 1f;
        private readonly Color _originalColor = new Color(2.313f, 1.724f, 0.731f, 1.000f);
        private readonly Color _fadedColor = new Color(1.498039f, 1.116989f, 0.4734369f, 1.000f);
        private static readonly int GradientStrengthId = Shader.PropertyToID("_GradientStrength");


        public OrbGlowingEffect()
        {
            ShaderID_1 = Shader.PropertyToID("_BorderScale");
            IsUsingFloat = false;
            MinVec4 = new Vector4(0, 0, 0, 3);
            MaxVec4 = new Vector4(XMax, 0, 0, WMax);
        }
        
        public void FadeColor(float fadeTime, bool isFadeOut)
        {
            foreach (var material in Materials)
            {
                material.SetFloat(GradientStrengthId, isFadeOut ? FadedGradient : OriginalGradient);
                material.DOColor(isFadeOut ? _fadedColor : _originalColor, "_TintColor", fadeTime);
            }
        }
    }
}