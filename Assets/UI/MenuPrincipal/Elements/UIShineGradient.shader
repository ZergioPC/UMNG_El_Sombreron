Shader "CanvsSky/UIShineGradient"
{
    Properties
    {
        _ColorA ("Color A (Principal)", Color) = (1,0,0,1)
        _ColorB ("Color B (Secundario)", Color) = (0,0,1,1)
        
        _ColorSpeed ("Velocidad Cambio de Color", Range(0.1, 10.0)) = 1.0
        _AlphaSpeed ("Velocidad Oscilacion Transparencia", Range(0.1, 10.0)) = 2.0
        
        _MinAlpha ("Transparencia Minima (Abajo)", Range(0.0, 1.0)) = 0.2
        _MaxAlpha ("Transparencia Maxima (Abajo)", Range(0.0, 1.0)) = 1.0
    }
    SubShader
    {
        Tags 
        { 
            "Queue"="Transparent" 
            "RenderType"="Transparent" 
            "IgnoreProjector"="True"
        }
        
        LOD 100

        // Configuración para permitir transparencia Alpha Blending
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
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            fixed4 _ColorA;
            fixed4 _ColorB;
            float _ColorSpeed;
            float _AlphaSpeed;
            float _MinAlpha;
            float _MaxAlpha;

            // Función pseudo-aleatoria basada en seno/ruido 1D
            float noise1D(float t)
            {
                return frac(sin(t * 12.9898) * 43758.5453123);
            }

            // Interpolación suave para transiciones aleatorias continuas
            float smoothNoise1D(float t)
            {
                float i = floor(t);
                float f = frac(t);
                f = f * f * (3.0 - 2.0 * f); // Interpolación Smoothstep
                return lerp(noise1D(i), noise1D(i + 1.0), f);
            }

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // 1. Oscilación aleatoria suave del color (Ruido continuo con el tiempo)
                float colorNoise = smoothNoise1D(_Time.y * _ColorSpeed);
                fixed4 colorInterp = lerp(_ColorA, _ColorB, colorNoise);

                // 2. Gradiente vertical base (1 en la base / 0 en la parte superior)
                float verticalGradient = 1.0 - i.uv.y;

                // 3. Oscilación aleatoria del límite máximo de opacidad
                float alphaNoise = smoothNoise1D(_Time.y * _AlphaSpeed + 50.0); // Offset para desacoplar del color
                float dynamicMaxAlpha = lerp(_MinAlpha, _MaxAlpha, alphaNoise);

                // 4. Combinación final del color con el canal Alpha modulado
                fixed4 finalColor = colorInterp;
                finalColor.a = verticalGradient * dynamicMaxAlpha;

                return finalColor;
            }
            ENDCG
        }
    }
}