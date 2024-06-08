using System.Linq;
using UnityEngine;

namespace Code.Effects
{
    public abstract class EffectBase
    {
        protected int ShaderID_1;
        protected int ShaderID_2;
        
        protected Material Material;
        protected string MaterialName;
        protected Vector4 MinVec4;
        protected Vector4 MaxVec4;
        protected float MinFloat;
        protected float MaxFloat;
        protected bool IsUsingFloat;
        
        public void SetMaterial(Transform objectTransform)
        {
            var rend = objectTransform.GetComponentsInChildren<Renderer>()
                .First(x => x.material.name.Contains(MaterialName));
        
            Material = rend.material;
        }

        public void SetEffectByNormalizedValue(float t)
        {
            if (!Material)
            {
                return;
            }
            
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
            Material.SetVector(ShaderID_1, interpolatedVal);
        }
    
        private void SetInterpolatedVal(float min, float max, float t)
        {
            var interpolatedVal = Mathf.Lerp(min, max, t);
            var id = Shader.PropertyToID("_Distortion");
            Material.SetFloat(ShaderID_1, interpolatedVal);
        }
    }
}