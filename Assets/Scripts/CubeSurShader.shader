Shader "Custom/CubeSurShader"
{
    Properties
    {
       _Smoothness ("Smoothness", Range(0.0, 1.0)) = 0.5
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 200

        CGPROGRAM
        #pragma surface ConfigureSurface Standard fullforwardshadows addshadow
        #pragma instancing_options assumeuniformscaling procedural:ConfigureProcedural
        #pragma target 4.5

        float _Smoothness;
#if defined(UNITY_PROCEDURAL_INSTANCING_ENABLED)
        StructuredBuffer<float4> _Positions;
        float _Step;
#endif

        struct Input {
            float3 worldPos;
        };

        void ConfigureProcedural() {
#if defined(UNITY_PROCEDURAL_INSTANCING_ENABLED)
			float3 position = _Positions[unity_InstanceID].xyz;
            unity_ObjectToWorld = 0.0f;
            // 设置Transform矩阵的位移列
            unity_ObjectToWorld._m03_m13_m23_m33 = float4(position, 1.0);
            unity_ObjectToWorld._m00_m11_m22 = _Step;
#endif
        }

        void ConfigureSurface (Input IN, inout SurfaceOutputStandard o) {
            o.Albedo = saturate(IN.worldPos * 0.5f + 0.5f);
            o.Smoothness = _Smoothness;
        }
        ENDCG
    }
    FallBack "Diffuse"
}