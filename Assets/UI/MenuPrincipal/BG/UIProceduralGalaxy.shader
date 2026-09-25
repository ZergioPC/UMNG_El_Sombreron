Shader "CanvsSky/UIProceduralGalaxy"
{
    Properties
    {
        _Color ("Galaxy Color", Color) = (0.35, 0.55, 1.0, 1)
        _CoreColor ("Core Color", Color) = (1.0, 0.75, 0.35, 1)

        _GalaxySize ("Galaxy Size", Range(0.1, 2.0)) = 0.8
        _CoreSize ("Core Size", Range(0.01, 0.5)) = 0.12
        _ArmCount ("Arm Count", Range(1, 8)) = 3
        _ArmWidth ("Arm Width", Range(0.01, 1.0)) = 0.25
        _Rotation ("Rotation", Range(-10, 10)) = 1.0
        _Speed ("Rotation Speed", Range(-2, 2)) = 0.05

        _Brightness ("Brightness", Range(0, 5)) = 1.5
        _Falloff ("Falloff", Range(0.1, 5)) = 1.5
        _NoiseAmount ("Noise", Range(0, 2)) = 0.5
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "IgnoreProjector"="True"
            "RenderType"="Transparent"
            "PreviewType"="Plane"
        }

        Cull Off
        Lighting Off
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

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

            fixed4 _Color;
            fixed4 _CoreColor;

            float _GalaxySize;
            float _CoreSize;
            float _ArmCount;
            float _ArmWidth;
            float _Rotation;
            float _Speed;
            float _Brightness;
            float _Falloff;
            float _NoiseAmount;

            v2f vert(appdata v)
            {
                v2f o;

                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;

                return o;
            }

            // --------------------------------------------------
            // Pseudo random
            // --------------------------------------------------

            float hash21(float2 p)
            {
                p = frac(p * float2(123.34, 456.21));
                p += dot(p, p + 45.32);

                return frac(p.x * p.y);
            }

            // --------------------------------------------------
            // Smooth noise
            // --------------------------------------------------

            float noise(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);

                f = f * f * (3.0 - 2.0 * f);

                float a = hash21(i);
                float b = hash21(i + float2(1, 0));
                float c = hash21(i + float2(0, 1));
                float d = hash21(i + float2(1, 1));

                return lerp(
                    lerp(a, b, f.x),
                    lerp(c, d, f.x),
                    f.y
                );
            }

            // --------------------------------------------------
            // Fractal noise
            // --------------------------------------------------

            float fbm(float2 p)
            {
                float value = 0.0;

                value += noise(p) * 0.5;
                p *= 2.0;

                value += noise(p) * 0.25;
                p *= 2.0;

                value += noise(p) * 0.125;
                p *= 2.0;

                value += noise(p) * 0.0625;

                return value;
            }

            // --------------------------------------------------
            // Fragment
            // --------------------------------------------------

            fixed4 frag(v2f i) : SV_Target
            {
                float2 uv = i.uv;

                // Convert UV to centered coordinates
                float2 p = uv - 0.5;

                // Correct aspect ratio
                p.x *= _ScreenParams.x / _ScreenParams.y;

                // Rotation animation
                float time = _Time.y * _Speed;

                float s = sin(time);
                float c = cos(time);

                p = float2(
                    p.x * c - p.y * s,
                    p.x * s + p.y * c
                );

                // Distance from center
                float radius = length(p);

                // Angle
                float angle = atan2(p.y, p.x);

                // --------------------------------------------------
                // Galaxy shape
                // --------------------------------------------------

                float normalizedRadius = radius / _GalaxySize;

                // Spiral deformation
                float spiralAngle =
                    angle +
                    normalizedRadius * _Rotation * 6.28318;

                // Create spiral arms
                float arm =
                    sin(
                        spiralAngle * _ArmCount
                    );

                // Convert to soft bands
                float arms =
                    pow(
                        saturate(arm * 0.5 + 0.5),
                        1.0 / max(_ArmWidth, 0.01)
                    );

                // --------------------------------------------------
                // Noise / dust
                // --------------------------------------------------

                float2 noiseUV =
                    p * 5.0 +
                    float2(
                        time * 0.05,
                        time * 0.02
                    );

                float dust =
                    fbm(noiseUV);

                // Add small scale detail
                float detail =
                    noise(p * 18.0);

                dust =
                    lerp(
                        1.0,
                        dust,
                        _NoiseAmount
                    );

                // --------------------------------------------------
                // Galaxy density
                // --------------------------------------------------

                float galaxy =
                    arms *
                    dust;

                // Fade toward the outside
                float radialFade =
                    exp(
                        -normalizedRadius *
                        _Falloff
                    );

                galaxy *= radialFade;

                // --------------------------------------------------
                // Core
                // --------------------------------------------------

                float core =
                    exp(
                        -radius *
                        radius /
                        (_CoreSize * _CoreSize)
                    );

                // --------------------------------------------------
                // Combine colors
                // --------------------------------------------------

                float3 galaxyColor =
                    _Color.rgb *
                    galaxy *
                    _Brightness;

                float3 coreColor =
                    _CoreColor.rgb *
                    core *
                    _Brightness *
                    2.0;

                float3 finalColor =
                    galaxyColor +
                    coreColor;

                // --------------------------------------------------
                // Alpha
                // --------------------------------------------------

                float alpha =
                    saturate(
                        galaxy +
                        core
                    );

                return fixed4(
                    finalColor,
                    alpha
                );
            }

            ENDCG
        }
    }
}