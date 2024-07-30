using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using UnityEngine;

namespace Code.Effects
{
    public abstract class EffectBase
    {
        protected int ShaderID_1;
        protected int ShaderID_2;

        protected string MaterialName;
        protected List<Material> Materials = new List<Material>();
        protected Vector4 MinVec4;
        protected Vector4 MaxVec4;
        protected float MinFloat;
        protected float MaxFloat;
        protected bool IsUsingFloat;
        protected Material Material => Materials[0];

        public void SetMaterial(GameObject go)
        {
            if (string.IsNullOrEmpty(MaterialName))
            {
                Materials = go.GetComponentsInChildren<Renderer>().Select(x => x.material).ToList();
            }
            else
            {
                var renderers = go.GetComponentsInChildren<Renderer>();
                foreach (var renderer in renderers)
                {
                    foreach (var material in renderer.materials)
                    {
                        if (material.name.Contains(MaterialName))
                        {
                            Materials.Add(material);
                        }
                    }     
                }
            }
        }

        public void FadeAlpha(float duration, bool isFadeOut)
        {
            foreach (var material in Materials)
            {
                var color = material.GetColor(ShaderID_1);
                color.a = isFadeOut ? 0 : 1;
                material.DOColor(color, ShaderID_1, duration);
            }
        }
        
        public void FadeEffect(float fadeTime)
        {
            foreach (var material in Materials)
            {
                if (IsUsingFloat)
                {
                    material.DOFloat(MinFloat, ShaderID_1, fadeTime);
                }
                else
                {
                    material.DOVector(MinVec4, ShaderID_1, fadeTime);
                }
            }
        }

        public void SetEffectByNormalizedValue(float t)
        {
            
            if (IsUsingFloat)
            {
                SetInterpolatedVal(MinFloat, MaxFloat, t);
            }
            else
            {
                SetInterpolatedVal(MinVec4, MaxVec4, t);
            }
        }
        
        private void SetInterpolatedVal(Vector4 min, Vector4 max, float t)
        {
            var interpolatedVal = Vector4.Lerp(min, max, t);
            foreach (var material in Materials)
            {
                material.SetVector(ShaderID_1, interpolatedVal);
            }
        }
    
        private void SetInterpolatedVal(float min, float max, float t)
        {
            var interpolatedVal = Mathf.Lerp(min, max, t);
            foreach (var material in Materials)
            {
                material.SetFloat(ShaderID_1, interpolatedVal);
            }
        }
    }
}