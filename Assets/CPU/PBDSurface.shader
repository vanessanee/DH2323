Shader "Custom/PBDSurface"
{
    Properties
    {
        _Color ("Color", Color) = (0.2, 0.6, 1.0, 1.0)
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
            #pragma target 4.5

            #include "UnityCG.cginc"

            struct vertData
            {
                float3 pos   : POSITION;
                float2 uvs   : TEXCOORD0;
                float3 norms : NORMAL;
            };

            StructuredBuffer<vertData> vertsBuff;
            StructuredBuffer<int>      triBuff;
            float4x4 TRSMatrix;
            float4x4 invTRSMatrix;
            fixed4   _Color;

            struct v2f
            {
                float4 pos     : SV_POSITION;
                float3 worldN  : TEXCOORD0;
            };

            v2f vert (uint id : SV_VertexID)
            {
                int      idx  = triBuff[id];
                vertData vd   = vertsBuff[idx];

                float4 worldPos = mul(TRSMatrix, float4(vd.pos, 1.0));
                float3 worldN   = normalize(mul((float3x3)TRSMatrix, vd.norms));

                v2f o;
                o.pos    = UnityWorldToClipPos(worldPos);
                o.worldN = worldN;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // Simple lambert lighting
                float3 lightDir = normalize(float3(0.5, 1.0, 0.5));
                float  diff     = max(0.2, dot(normalize(i.worldN), lightDir));
                return fixed4(_Color.rgb * diff, 1.0);
            }
            ENDCG
        }
    }
}
