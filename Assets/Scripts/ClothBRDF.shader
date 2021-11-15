Shader "Custom/ClothBRDF" {
    Properties {
        _MainTex ("Texture", 2D) = "white" {}
        _Alpha ("Alpha", Float) = 0.5
        _K_amp ("K_amp", Float) = 0.5
        // _Glossiness ("Glossiness", Float) = 0.5
        _Metallicness ("Metallicness", Float) = 0.5
    }

    SubShader {
        Tags {"RenderType"="Opaque"}

        Pass {
            Tags {"LightMode"="ForwardBase"}
            CULL OFF

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 5.0
            #pragma multi_compile_fwbase

            #include "UnityCG.cginc"
            #include "Lighting.cginc"
            #include "UnityStandardBRDF.cginc"

            struct v2f {
                float2 uv : TEXCOORD0;
                float3 normal : TEXCOORD1;
                float3 worldPos : TEXCOORD2;
                float4 pos : SV_POSITION;
            };

            StructuredBuffer<int> Trimap;
            StructuredBuffer<float4> Position;
            StructuredBuffer<float2> Texcoord;
            StructuredBuffer<float4> Normal;
            int verCountPerCol;
            int verCountPerRow;
            sampler2D _MainTex;
            float4 _MainTex_ST;
            float _Alpha;
            float _K_amp;
            // float _Glossiness;
            float _Metallicness;


            float3 Schlick_F(float3 F0, float LdotH) {
                float3 unit = float3(1.0, 1.0, 1.0);
                return F0 + (unit - F0) * Pow5(1.0 - LdotH);
            }

            float Cloth_D(float alpha, float k_amp, float NdotH) {
                float a2 = alpha * alpha;
                float d2 = NdotH * NdotH;
                float fact = UNITY_INV_PI * (1 - step(NdotH, 0)) / (1 + k_amp * a2);
                return fact * (1 + (k_amp * exp(d2 / (a2 * (d2 - 1)))) / pow(1 - d2, 2));
            }

            float3 My_BRDF(float NdotL, float NdotV, float VdotH, float NdotH, float3 LdotH, float3 albedo, float3 F0) {
                float3 F = Schlick_F(F0, LdotH);
                float D = Cloth_D(_Alpha, _K_amp, NdotH);
                float3 unit = float3(1.0, 1.0, 1.0);

                float3 diff = UNITY_INV_PI * (unit - F) * albedo;
                float3 spec = F * D / (4 * (NdotL + NdotV - NdotL * NdotV));
                
                return saturate(diff + spec);
            }

            v2f vert(uint id : SV_VertexID) {
                v2f o;
                int triId = Trimap[id];
                float4 worldPos = Position[triId];
                o.pos = UnityWorldToClipPos(worldPos);
                o.uv = TRANSFORM_TEX(Texcoord[triId], _MainTex);
                o.normal = Normal[triId].xyz;
                o.worldPos = worldPos.xyz;

                return o;
            }

            half4 frag(v2f i) : SV_Target {
                float4 mainTex = tex2D(_MainTex, i.uv);

                // Vectors
                float3 L = UnityWorldSpaceLightDir(i.worldPos);
                float3 V = normalize(_WorldSpaceLightPos0.xyz - i.worldPos.xyz);
                float3 H = Unity_SafeNormalize(L + V);
                float3 N = i.normal;

                float NdotL = saturate(dot(N, L));
                float NdotH = saturate(dot(N, H));
                float NdotV = saturate(dot(N, V));
                float VdotH = saturate(dot(V, H));
                float LdotH = saturate(dot(L, H));

                // float roughness = pow(1 - _Glossiness, 2);

                half oneMinusReflectivity;
                half3 specColor;        // F0
                float3 albedo = DiffuseAndSpecularFromMetallic(mainTex.rgb, _Metallicness, specColor, oneMinusReflectivity);

                float3 directLight = My_BRDF(NdotL, NdotV, VdotH, NdotH, LdotH, albedo, specColor) * NdotL;

                float col = float4(directLight * _LightColor0.rgb, 1);
                col += float4(UNITY_LIGHTMODEL_AMBIENT.xyz * albedo, 1);

                return col;
            }

            ENDCG
        }
    }
}