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
        private readonly Color _fadedPuzzleColor = new Color(0.5676508f, 0.4232598f, 0.1793991f, 1.000f);
        private static readonly int GradientStrengthId = Shader.PropertyToID("_GradientStrength");


        public OrbGlowingEffect()
        {
            ShaderID_1 = Shader.PropertyToID("_BorderScale");
            ShaderID_2 = Shader.PropertyToID("_TintColor");
            IsUsingFloat = false;
            MinVec4 = new Vector4(0, 0, 0, 3);
            MaxVec4 = new Vector4(XMax, 0, 0, WMax);
        }
        
        public void FadeColor(float fadeTime, bool isFadeOut)
        {
            Material.SetFloat(GradientStrengthId, isFadeOut ? FadedGradient : OriginalGradient);
            Material.DOColor(isFadeOut ? _fadedColor : _originalColor, ShaderID_2, fadeTime);
        }

        public void IncreaseAlphaToOne(float duration)
        {
            foreach (var material in Materials)
            {
                material.DOColor(_fadedPuzzleColor, ShaderID_2, duration);
            }
        }

        public void SetAlphaZero()
        {
            var fadedAlphaZero = _fadedPuzzleColor;
            fadedAlphaZero.a = 0;
            foreach (var material in Materials)
            {
                material.SetColor(ShaderID_2, fadedAlphaZero);
            }
        }

        public void InterpolatePuzzleColor(float t)
        {
            var interpolatedColor = Vector4.Lerp(_fadedPuzzleColor, _originalColor, t);
            
            foreach (var material in Materials)
            {
                var color = material.GetColor(ShaderID_2);
                material.SetColor(ShaderID_2, interpolatedColor);
            }
        }
    }
}