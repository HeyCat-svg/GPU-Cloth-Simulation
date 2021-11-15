Shader "Custom/DiffuseWithShadows"{
    Properties{
        [NoScaleOffset] _MainTex ("Texture", 2D) = "white" {}
    }

    SubShader {
        Tags { "RenderType"="Opaque" }

        Pass {
            Tags {"LightMode"="ForwardBase"}
            CULL OFF

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 5.0
            #pragma multi_compile_fwdbase

            #include "UnityCG.cginc"
            #include "Lighting.cginc"

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

            v2f vert(uint id : SV_VertexID) {
                v2f o;
                int triId = Trimap[id];
                float4 worldPos = Position[triId];
                o.pos = UnityWorldToClipPos(worldPos);
                // o.pos = mul(UNITY_MATRIX_VP, worldPos);
                o.uv = Texcoord[triId];
                o.normal = Normal[triId].xyz;
                o.worldPos = worldPos.xyz;

                return o;
            }

            half4 frag(v2f i) : SV_Target {
                half4 albedo = tex2D(_MainTex, i.uv);

                float3 ambient = ShadeSH9(float4(i.normal, 1)).rgb;
                float3 diff = max(dot(i.normal, UnityWorldSpaceLightDir(i.worldPos)), 0) * _LightColor0.rgb;
                
                half3 col = (ambient + diff) * albedo.rgb;

                return half4(col, 1.0f);
            }

            ENDCG
        }
    }
}