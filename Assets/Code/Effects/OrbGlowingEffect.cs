using UnityEngine;

namespace Code.Effects
{
    public class OrbGlowingEffect : EffectBase
    {
        private const float XMax = 15;
        private const float WMax = 15;
    
        public OrbGlowingEffect()
        {
            MaterialName = "Mesh_MeshEffect 1";
            ShaderID_1 = Shader.PropertyToID("_BorderScale");
            IsUsingFloat = false;
            MinVec4 = new Vector4(0, 0, 0, 3);
            MaxVec4 = new Vector4(XMax, 0, 0, WMax);
        }
    }
}