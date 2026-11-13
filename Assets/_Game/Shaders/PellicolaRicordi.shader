// R007 tappa 3: pellicola dei ricordi. Passo a schermo intero leggero dopo il post-processing:
// gate weave (micro-tremolio del fotogramma a 12 fps), sfarfallio di esposizione, polvere e graffi rari.
// Intensita' globale: _TrenoPellicola (0 = spenta, 1 = ricordi) per _TrenoPellicolaQ (qualita' del preset).
// Con intensita' 0 restituisce l'immagine com'e'. Vignetta, grana, alone e curva stanno nei Volume profile (VP_Flashback).
Shader "Treno/PellicolaRicordi"
{
    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" }
        ZWrite Off ZTest Always Cull Off Blend Off

        Pass
        {
            Name "PellicolaRicordi"

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            float _TrenoPellicola;
            float _TrenoPellicolaQ;
            float _TrenoPellicolaMov; // R008: 1 = "riduci effetti di movimento" (niente tremolio del fotogramma ne' sfarfallio)

            float h11(float p)
            {
                p = frac(p * 0.1031);
                p *= p + 33.33;
                p *= p + p;
                return frac(p);
            }

            half4 Frag(Varyings i) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
                float2 uv = i.texcoord;
                float k = _TrenoPellicola * _TrenoPellicolaQ;
                if (k <= 0.001)
                    return SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv);

                float t = floor(_Time.y * 12.0);
                // gate weave: lo scatto del fotogramma sul proiettore (frazioni di percento dello schermo)
                uv += float2(h11(t * 1.7) - 0.5, h11(t * 2.3 + 9.0) - 0.5) * float2(0.0016, 0.0022) * k * (1.0 - _TrenoPellicolaMov);
                half3 c = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv).rgb;
                // sfarfallio di esposizione (+-2,5%)
                c *= 1.0 + (h11(t * 3.1) - 0.5) * 0.05 * k * (1.0 - _TrenoPellicolaMov);

                // polvere: fino a 4 puntini per fotogramma, in media meno di uno
                float aspect = _ScreenParams.x / _ScreenParams.y;
                [unroll] for (int n = 0; n < 4; n++)
                {
                    float sd = t * 7.0 + n * 13.0;
                    if (h11(sd) > 0.62)
                    {
                        float2 p = float2(h11(sd + 1.0), h11(sd + 2.0));
                        float r = lerp(0.0014, 0.0038, h11(sd + 3.0));
                        float2 d = (i.texcoord - p) * float2(aspect, 1.0);
                        float m = smoothstep(r, r * 0.4, length(d));
                        half3 col = h11(sd + 4.0) > 0.5 ? half3(0.88, 0.82, 0.66) : half3(0.03, 0.02, 0.012);
                        c = lerp(c, col, m * 0.65 * k);
                    }
                }

                // graffio verticale: raro, sottile, interrotto
                float st = floor(_Time.y * 8.0);
                if (h11(st * 5.1) > 0.975)
                {
                    float x = h11(st * 7.7);
                    float d = abs(i.texcoord.x - x);
                    float ln = smoothstep(0.00035, 0.0, d) * step(h11(floor(i.texcoord.y * 6.0) + st), 0.8);
                    c = lerp(c, half3(0.85, 0.80, 0.64), ln * 0.16 * k);
                }
                return half4(c, 1.0);
            }
            ENDHLSL
        }
    }
}
