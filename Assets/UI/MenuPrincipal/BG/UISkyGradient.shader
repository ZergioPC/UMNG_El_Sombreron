Shader "CanvsSky/UISkyGradient"
{
    Properties
    {
        _TopColor ("Top Color", Color) = (0.02, 0.01, 0.10, 1)
        _MiddleColor ("Middle Color", Color) = (0.15, 0.03, 0.25, 1)
        _BottomColor ("Bottom Color", Color) = (0.005, 0.005, 0.02, 1)
    }

    SubShader
    {
        Tags
        {
            "RenderType"="Opaque"
            "Queue"="Background"
        }

        Cull Off
        ZWrite Off
        ZTest Always

        Pass
        {
            CGPROGRAM

            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            fixed4 _TopColor;
            fixed4 _MiddleColor;
            fixed4 _BottomColor;

            v2f vert(appdata v)
            {
                v2f o;

                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;

                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float t = i.uv.y;

                fixed4 color;

                if (t < 0.5)
                {
                    color = lerp(_BottomColor, _MiddleColor, t * 2.0);
                }
                else
                {
                    color = lerp(_MiddleColor, _TopColor, (t - 0.5) * 2.0);
                }

                return color;
            }

            ENDCG
        }
    }
}