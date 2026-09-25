Shader "UI/Logo1Animated"
{
    Properties
    {
        [PerRendererData] _MainTex ("Logo", 2D) = "white" {}

        _GoldColor ("Gold Color", Color) = (1.0, 0.55, 0.08, 1)
        _BrightGold ("Bright Gold", Color) = (1.0, 0.9, 0.35, 1)

        _ShineSpeed ("Shine Speed", Range(0, 5)) = 0.8
        _ShineWidth ("Shine Width", Range(0.01, 1)) = 0.15
        _ShineIntensity ("Shine Intensity", Range(0, 5)) = 2.0

        _ShineAngle ("Shine Angle", Range(-180, 180)) = -25

        _GlowIntensity ("Glow Intensity", Range(0, 5)) = 1.2

        _PulseSpeed ("Pulse Speed", Range(0, 5)) = 1.0
        _PulseAmount ("Pulse Amount", Range(0, 1)) = 0.15

        _Metallic ("Metallic", Range(0, 1)) = 0.7
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "IgnoreProjector"="True"
            "RenderType"="Transparent"
            "CanUseSpriteAtlas"="True"
        }

        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off
        ZWrite Off
        ZTest Always

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

            float4 _GoldColor;
            float4 _BrightGold;

            float _ShineSpeed;
            float _ShineWidth;
            float _ShineIntensity;

            float _ShineAngle;

            float _GlowIntensity;

            float _PulseSpeed;
            float _PulseAmount;

            float _Metallic;


            v2f vert(appdata_t v)
            {
                v2f o;

                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.texcoord;
                o.color = v.color;

                return o;
            }


            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 tex = tex2D(_MainTex, i.uv);

                float alpha = tex.a * i.color.a;


                // ====================================================
                // PULSACIÓN
                // ====================================================

                float pulse =
                    sin(_Time.y * _PulseSpeed * 2.0) * 0.5 + 0.5;

                pulse = lerp(
                    1.0 - _PulseAmount,
                    1.0 + _PulseAmount,
                    pulse
                );


                // ====================================================
                // ORO BASE
                // ====================================================

                float luminance =
                    dot(tex.rgb, float3(0.299, 0.587, 0.114));

                float metallicVariation =
                    lerp(0.65, 1.25, luminance);

                float3 gold =
                    _GoldColor.rgb * metallicVariation;


                // ====================================================
                // DIRECCIÓN DEL BRILLO
                // ====================================================

                float angle =
                    radians(_ShineAngle);

                float2 direction =
                    float2(
                        cos(angle),
                        sin(angle)
                    );

                /*
                    Proyectamos la posición UV sobre la dirección
                    del movimiento.

                    Esto permite controlar el ángulo del brillo.
                */

                float position =
                    dot(i.uv - 0.5, direction);


                // ====================================================
                // MOVIMIENTO CONTINUO
                // ====================================================

                /*
                    El recorrido tiene un margen suficiente para que
                    el brillo pueda salir completamente del logo antes
                    de volver a entrar.

                    Esto elimina el salto visual.
                */

                float travel =
                    frac(_Time.y * _ShineSpeed);

                float shinePosition =
                    lerp(-0.85, 0.85, travel);


                float distanceFromShine =
                    abs(position - shinePosition);


                // ====================================================
                // BRILLO
                // ====================================================

                float shine =
                    1.0 - smoothstep(
                        0.0,
                        _ShineWidth,
                        distanceFromShine
                    );

                shine *= shine;


                // ====================================================
                // DESVANECIMIENTO EN LOS EXTREMOS
                // ====================================================

                /*
                    El brillo comienza y termina fuera de la zona
                    visible. De esta forma el siguiente ciclo comienza
                    cuando el anterior ya desapareció.
                */

                float fadeIn =
                    smoothstep(
                        -0.85,
                        -0.55,
                        shinePosition
                    );

                float fadeOut =
                    1.0 - smoothstep(
                        0.55,
                        0.85,
                        shinePosition
                    );

                shine *= fadeIn * fadeOut;


                // ====================================================
                // COLOR DEL DESTELLO
                // ====================================================

                float3 shineColor =
                    _BrightGold.rgb *
                    shine *
                    _ShineIntensity;


                // ====================================================
                // GRADIENTE METÁLICO
                // ====================================================

                float verticalGradient =
                    smoothstep(0.0, 1.0, i.uv.y);

                gold +=
                    _BrightGold.rgb *
                    verticalGradient *
                    0.15;


                // ====================================================
                // METAL
                // ====================================================

                gold = lerp(
                    gold,
                    gold * _BrightGold.rgb,
                    _Metallic * 0.15
                );


                // ====================================================
                // RESULTADO
                // ====================================================

                float3 finalColor =
                    gold + shineColor;

                finalColor *= pulse;


                // ====================================================
                // GLOW
                // ====================================================

                float3 glow =
                    _BrightGold.rgb *
                    shine *
                    _GlowIntensity;

                finalColor += glow * 0.25;


                return fixed4(
                    finalColor,
                    alpha
                );
            }

            ENDCG
        }
    }
}