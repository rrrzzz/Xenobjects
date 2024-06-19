Shader "VanishingStandardShader"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
        _Glossiness ("Smoothness", Range(0.0, 1.0)) = 0.5
        _Metallic ("Metallic", Range(0.0, 1.0)) = 0.0
        _ClipStart ("Clip Start", Float) = 0.0
        _ClipEnd ("Clip End", Float) = 1.0
        _ClipCoordinate ("Clip Coordinate", Float) = 0.0
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 200

        CGPROGRAM
        #pragma surface surf Standard fullforwardshadows

        half _Color;
        half _Glossiness;
        half _Metallic;
        half _ClipStart;
        half _ClipEnd;
        half _ClipCoordinate;

        struct Input {
            float2 uv_MainTex;
            float3 worldPos;
        };

        // Add the Standard surface output structure
        void surf (Input IN, inout SurfaceOutputStandard o)
        {
            o.Albedo = _Color;

            // Metallic and smoothness
            o.Metallic = _Metallic;
            o.Smoothness = _Glossiness;
            
            // Transform vertex to local space
            float3 localPos = IN.worldPos - mul(unity_ObjectToWorld, float4(0, 0, 0, 1)).xyz;
            if (_ClipCoordinate < 0.33)
            {
                if (localPos.x < _ClipStart || localPos.x > _ClipEnd)
                {
                    clip(-1);
                }
            }
            else if (_ClipCoordinate < 0.66)
            {
                if (localPos.y < _ClipStart || localPos.y > _ClipEnd)
                {
                    clip(-1);
                }
            }
            else
            {
                if (localPos.z < _ClipStart || localPos.z > _ClipEnd)
                {
                    clip(-1);
                }
            }            
        }
        ENDCG
    }
    FallBack "Diffuse"
}