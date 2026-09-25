Shader "CanvsSky/UISkyColorShader"
{
    Properties
    {
        [PerRendererData] _MainTex ("Galaxy Texture", 2D) = "white" {}
        _Color ("Base Tint", Color) = (1,1,1,1)
        _Color1 ("Pulse Color A", Color) = (0.1, 0.2, 0.8, 1.0)
        _Color2 ("Pulse Color B", Color) = (0.8, 0.1, 0.5, 1.0)
        _ColorSpeed ("Pulse Speed", Float) = 2.0
        _Exposure ("Exposure", Range(0,8)) = 1

        // UI Masking standard properties
        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        _ColorMask ("Color Mask", Float) = 15
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "IgnoreProjector"="True"
            "RenderType"="Transparent"
            "CanUseSpriteAtlas"="True"
            "PreviewType"="Plane"
        }

        // Stencil settings for UI RectMask2D and Mask components
        Stencil
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp]
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }

        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]

        Pass
        {
            CGPROGRAM

            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata_t
            {
                float4 vertex   : POSITION;
                float2 texcoord : TEXCOORD0;
                float4 color    : COLOR;
            };

            struct v2f
            {
                float4 vertex   : SV_POSITION;
                float2 uv       : TEXCOORD0;
                float4 color    : COLOR;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;

            fixed4 _Color;
            fixed4 _Color1;
            fixed4 _Color2;
            float _ColorSpeed;
            float _Exposure;

            v2f vert(appdata_t v)
            {
                v2f o;

                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.texcoord, _MainTex);
                o.color = v.color; // Contains UI Graphic element color/alpha

                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 col = tex2D(_MainTex, i.uv);

                // 1. Calculate color pulse over time
                float t = (sin(_Time.y * _ColorSpeed) + 1.0) * 0.5;
                float3 galaxyColor = lerp(_Color1.rgb, _Color2.rgb, t);

                // 2. Apply base color tint + pulsing galaxy color
                col.rgb *= _Color.rgb * galaxyColor;

                // 3. Apply exposure boost
                col.rgb *= _Exposure;

                // 4. Combine vertex alpha & canvas tinting from UI system
                col *= i.color;

                return col;
            }

            ENDCG
        }
    }
    FallBack "UI/Default"
}