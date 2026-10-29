Shader "CanvsSky/UIRuanaDeform"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)

        // General Masking Settings
        _SafeCenter ("Center (UV)", Vector) = (0.5, 0.5, 0, 0)
        _SafeRadius ("Safe Radius", Float) = 0.1
        _WaveGrowth ("Growth Factor", Float) = 1.0

        // Radial Wave Settings
        _RadialAmp ("Radial Amplitude", Float) = 0.02
        _RadialFreq ("Radial Frequency", Float) = 20.0
        _RadialSpeed ("Radial Speed", Float) = 2.0

        // Horizontal Wave Settings
        _HorizAmp ("Horizontal Amplitude", Float) = 0.02
        _HorizFreq ("Horizontal Frequency", Float) = 15.0
        _HorizSpeed ("Horizontal Speed", Float) = 3.0

        // UI Masking Properties
        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        _ColorMask ("Color Mask", Float) = 15
        [Toggle(UNITY_UI_ALPHATEST)] _UseUIAlphaClip ("Use Alpha Clip", Float) = 0
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "IgnoreProjector"="True"
            "RenderType"="Transparent"
            "PreviewType"="Plane"
            "CanUseSpriteAtlas"="True"
        }

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
            Name "Default"
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0

            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            #pragma multi_compile __ UNITY_UI_ALPHATEST
            #pragma multi_compile __ UNITY_UI_CLIP_RECT

            struct appdata_t
            {
                float4 vertex   : POSITION;
                float4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex   : SV_POSITION;
                fixed4 color    : COLOR;
                float2 texcoord  : TEXCOORD0;
                float4 worldPosition : TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            fixed4 _Color;
            fixed4 _TextureSampleAdd;
            float4 _ClipRect;

            // Mask parameters
            float4 _SafeCenter;
            float _SafeRadius;
            float _WaveGrowth;

            // Wave parameters
            float _RadialAmp;
            float _RadialFreq;
            float _RadialSpeed;

            float _HorizAmp;
            float _HorizFreq;
            float _HorizSpeed;

            v2f vert(appdata_t v)
            {
                v2f OUT;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);
                
                OUT.worldPosition = v.vertex;
                OUT.vertex = UnityObjectToClipPos(OUT.worldPosition);
                OUT.texcoord = v.texcoord;
                OUT.color = v.color * _Color;
                
                return OUT;
            }

            sampler2D _MainTex;

            fixed4 frag(v2f IN) : SV_Target
            {
                float2 uv = IN.texcoord;

                // Distance from the designated center
                float2 dir = uv - _SafeCenter.xy;
                float dist = length(dir);

                // Calculate global attenuation factor (0 within safe zone, scaling upward outside)
                float waveMask = 0.0;
                if (dist > _SafeRadius)
                {
                    float effectiveDist = dist - _SafeRadius;
                    waveMask = pow(effectiveDist, _WaveGrowth);
                }

                // Apply deformers scaled by waveMask
                if (waveMask > 0.0)
                {
                    // 1. Radial Deformation
                    float radialWave = sin(dist * _RadialFreq - _Time.y * _RadialSpeed) * _RadialAmp * waveMask;
                    uv += normalize(dir) * radialWave;

                    // 2. Horizontal Deformation
                    float horizWave = sin(uv.y * _HorizFreq + _Time.y * _HorizSpeed) * _HorizAmp * waveMask;
                    uv.x += horizWave;
                }

                // Sample Texture
                half4 color = (tex2D(_MainTex, uv) + _TextureSampleAdd) * IN.color;

                #ifdef UNITY_UI_CLIP_RECT
                color.a *= UnityGet2DClipping(IN.worldPosition.xy, _ClipRect);
                #endif

                #ifdef UNITY_UI_ALPHATEST
                clip (color.a - 0.001);
                #endif

                return color;
            }
            ENDCG
        }
    }
}