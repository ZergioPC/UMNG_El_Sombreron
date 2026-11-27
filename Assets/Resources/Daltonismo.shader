Shader "Juego/Daltonismo"
{
    Properties
    {
        _Modo ("Modo", Float) = 0
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" }
        ZWrite Off Cull Off ZTest Always

        Pass
        {
            Name "Daltonismo"

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            float _Modo;

            float4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                float4 c = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, input.texcoord);

                int modo = (int)(_Modo + 0.5);
                if (modo == 0) return c;

                // Simulación de cada tipo (matrices de Machado, severidad completa)
                float3x3 m;
                if (modo == 1) // Protanopia
                    m = float3x3(0.152286, 1.052583, -0.204868,
                                 0.114503, 0.786281, 0.099216,
                                 -0.003882, -0.048116, 1.051998);
                else if (modo == 2) // Deuteranopia
                    m = float3x3(0.367322, 0.860646, -0.227968,
                                 0.280085, 0.672501, 0.047413,
                                 -0.011820, 0.042940, 0.968881);
                else // Tritanopia
                    m = float3x3(1.255528, -0.076749, -0.178779,
                                 -0.078411, 0.930809, 0.147602,
                                 0.004733, 0.691367, 0.303900);

                float3 simulado = mul(m, c.rgb);
                float3 error = c.rgb - simulado; // Lo que la persona no distingue

                // Corrección: ese error se traslada a canales que sí se perciben
                float3 correccion;
                if (modo == 3) correccion = float3(error.b, error.b * 0.7, 0.0);
                else correccion = float3(0.0, error.r * 0.7 + error.g, error.r * 0.7 + error.b);

                return float4(saturate(c.rgb + correccion * 1.5), c.a);
            }
            ENDHLSL
        }
    }
}