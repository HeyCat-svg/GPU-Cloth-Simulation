Shader "Unlit/ColorShader"
{
    Properties
    {
        _Color("Color Tint", Color) = (1, 1, 1, 1)
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            // make fog work
            #pragma multi_compile_fog

            #include "UnityCG.cginc"
            #include "Lighting.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float4 tangent : TANGENT;
            };

            struct v2f
            {
                UNITY_FOG_COORDS(1)
                float4 vertex : SV_POSITION;
                float3 normal : TEXCOORD0;
                float3 lightDir : TEXCOORD1;
                float3 viewDir : TEXCOORD2;
            };
            
            float4 _Color;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.normal = mul(transpose((float3x3)unity_WorldToObject), v.normal);
                o.normal = normalize(o.normal);
                UNITY_TRANSFER_FOG(o,o.vertex);

                TANGENT_SPACE_ROTATION;
                o.lightDir = mul(rotation, ObjSpaceLightDir(v.vertex)).xyz;
                o.viewDir = mul(rotation, ObjSpaceViewDir(v.vertex)).xyz;
                return o;
            }

            float4 frag (v2f i) : SV_Target
            {
                float3 lightDir = normalize(i.lightDir);
                float3 lightColor = _LightColor0.rgb;

                float3 diffuse = _Color.rgb * lightColor * DotClamped(lightDir, i.normal);
                fixed3 ambient = _Color.rgb * UNITY_LIGHTMODEL_AMBIENT.xyz;
                return float4((diffuse+ ambient)*2,1);
            }
            ENDCG
        }
    }
}
