// Agua estilizada de RECOLEC(ECO). Las olas usan los mismos parámetros que Waves.cs
// (se pasan como variables globales), así los objetos flotantes coinciden con la superficie.
Shader "RECOLECECO/Water"
{
    Properties
    {
        _ShallowColor ("Color claro", Color) = (0.16, 0.65, 0.65, 1)
        _DeepColor ("Color profundo", Color) = (0.055, 0.34, 0.45, 1)
        _SkyColor ("Reflejo cielo", Color) = (0.58, 0.72, 0.79, 1)
        _DirtyColor ("Agua sucia", Color) = (0.30, 0.33, 0.20, 1)
        _OilColor ("Mancha", Color) = (0.10, 0.10, 0.08, 1)
        _FoamColor ("Espuma", Color) = (1, 1, 1, 1)
        _Gloss ("Brillo", Range(8, 512)) = 110
        _SpecStrength ("Especular", Range(0, 3)) = 0.85
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" "Queue"="Geometry" }

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

        CBUFFER_START(UnityPerMaterial)
            half4 _ShallowColor;
            half4 _DeepColor;
            half4 _SkyColor;
            half4 _DirtyColor;
            half4 _OilColor;
            half4 _FoamColor;
            half _Gloss;
            half _SpecStrength;
        CBUFFER_END

        // Globales (Waves.cs / EnvironmentProgress.cs)
        float _RecTime;
        float4 _RecWave0; // dirX, dirZ, amplitud, k
        float4 _RecWave1;
        float4 _RecWave2;
        float4 _RecWaveSpeed; // velocidades de fase
        float4 _RecSector0; // x, z, radio, suciedad
        float4 _RecSector1;
        float4 _RecSector2;

        float WaveH(float4 w, float s, float2 p, out float2 grad)
        {
            float2 d = normalize(w.xy);
            float ph = w.w * dot(d, p) + s * _RecTime;
            grad = w.z * w.w * cos(ph) * d;
            return w.z * sin(ph);
        }

        float WaveHeight(float2 p, out float2 grad)
        {
            float2 g0, g1, g2;
            float h = WaveH(_RecWave0, _RecWaveSpeed.x, p, g0)
                    + WaveH(_RecWave1, _RecWaveSpeed.y, p, g1)
                    + WaveH(_RecWave2, _RecWaveSpeed.z, p, g2);
            grad = g0 + g1 + g2;
            return h;
        }

        float Hash21(float2 p)
        {
            p = frac(p * float2(123.34, 456.21));
            p += dot(p, p + 45.32);
            return frac(p.x * p.y);
        }

        float Noise(float2 p)
        {
            float2 i = floor(p);
            float2 f = frac(p);
            float2 u = f * f * (3.0 - 2.0 * f);
            float a = Hash21(i);
            float b = Hash21(i + float2(1, 0));
            float c = Hash21(i + float2(0, 1));
            float d = Hash21(i + float2(1, 1));
            return lerp(lerp(a, b, u.x), lerp(c, d, u.x), u.y);
        }

        float Fbm(float2 p)
        {
            float v = 0.0;
            float a = 0.5;
            for (int i = 0; i < 3; i++)
            {
                v += a * Noise(p);
                p = p * 2.03 + 17.1;
                a *= 0.5;
            }
            return v;
        }

        float SectorDirt(float4 s, float2 p)
        {
            float d = distance(p, s.xy);
            return s.w * (1.0 - smoothstep(s.z * 0.55, s.z * 1.05, d));
        }

        struct Attributes
        {
            float4 positionOS : POSITION;
            float3 normalOS : NORMAL;
        };

        struct Varyings
        {
            float4 positionCS : SV_POSITION;
            float3 positionWS : TEXCOORD0;
            float3 normalWS : TEXCOORD1;
            float waveH : TEXCOORD2;
            float fogFactor : TEXCOORD3;
        };

        Varyings vert(Attributes v)
        {
            Varyings o;
            float3 ws = TransformObjectToWorld(v.positionOS.xyz);
            float2 grad;
            float h = WaveHeight(ws.xz, grad);
            ws.y += h;
            o.positionWS = ws;
            o.normalWS = normalize(float3(-grad.x, 1.0, -grad.y));
            o.waveH = h;
            o.positionCS = TransformWorldToHClip(ws);
            o.fogFactor = ComputeFogFactor(o.positionCS.z);
            return o;
        }
        ENDHLSL

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma multi_compile_fog
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            half4 frag(Varyings i) : SV_Target
            {
                float2 p = i.positionWS.xz;
                float t = _RecTime;

                // Ondulaciones cruzadas, con menos detalle en la distancia.
                float2 uv = float2(dot(p, float2(0.82, 0.57)), dot(p, float2(-0.43, 0.90)));
                float distanceToCamera = distance(i.positionWS, GetCameraPositionWS());
                float detail = 1.0 - smoothstep(28.0, 135.0, distanceToCamera);
                float rippleA = sin(uv.x * 4.4 + uv.y * 0.78 + t * 1.95 + Noise(p * 0.31) * 3.2);
                float rippleB = sin(uv.y * 6.6 - uv.x * 0.68 - t * 2.45);
                float3 n = normalize(i.normalWS + float3(rippleA, 0.0, rippleB) * (0.058 * detail));

                float3 V = normalize(GetCameraPositionWS() - i.positionWS);
                float fres = pow(1.0 - saturate(dot(n, V)), 4.3);
                float ampSum = max(0.001, _RecWave0.z + _RecWave1.z + _RecWave2.z);
                float crest = saturate(i.waveH / ampSum * 0.5 + 0.5);
                float bands = Noise(float2(uv.x * 0.21, uv.y * 0.47) + t * 0.11);
                float3 baseCol = lerp(_DeepColor.rgb, _ShallowColor.rgb,
                    saturate(crest * 0.52 + bands * 0.44));

                // Suciedad por sector (indicador del juego)
                float dirt = max(SectorDirt(_RecSector0, p), max(SectorDirt(_RecSector1, p), SectorDirt(_RecSector2, p)));
                float patch = Fbm(p * 0.09 + float2(t * 0.01, 0));
                baseCol = lerp(baseCol, _DirtyColor.rgb, saturate(dirt * (0.55 + 0.45 * patch)));
                float oil = smoothstep(0.58, 0.66, Fbm(p * 0.16 + 3.7 + float2(0, t * 0.015))) * saturate(dirt * 1.4);
                float3 sheen = 0.5 + 0.5 * cos(6.2831 * (patch * 2.0 + float3(0.0, 0.33, 0.67)));
                float3 oilCol = lerp(_OilColor.rgb, _OilColor.rgb + sheen * 0.07, 0.6);
                baseCol = lerp(baseCol, oilCol, oil * 0.85);

                float4 shadowCoord = TransformWorldToShadowCoord(i.positionWS);
                Light L = GetMainLight(shadowCoord);
                float atten = L.shadowAttenuation;
                float ndl = saturate(dot(n, L.direction));

                float3 col = baseCol * lerp(0.64, 1.04, ndl * lerp(0.58, 1.0, atten));
                float skyAmount = fres * 0.67 * (1.0 - dirt * 0.56);
                col = lerp(col, _SkyColor.rgb, skyAmount);

                float3 H = normalize(L.direction + V);
                float highlight = saturate(dot(n, H));
                float gloss = pow(highlight, _Gloss);
                float gleam = pow(highlight, 22.0) * 0.095;
                float spec = (gloss + gleam) * _SpecStrength * atten
                    * (1.0 - oil) * (1.0 - dirt * 0.5);
                col += spec * L.color;

                // Espuma quebrada en las crestas, manteniendo legibles los residuos.
                float broken = Noise(float2(uv.x * 1.8, uv.y * 0.8) + float2(t * 0.18, -t * 0.16));
                float foam = smoothstep(0.88, 0.965, crest + broken * 0.16)
                    * smoothstep(0.40, 0.73, broken) * (0.4 + 0.6 * detail)
                    * (1.0 - dirt * 0.5);
                col = lerp(col, _FoamColor.rgb, foam * 0.43);

                col = MixFog(col, i.fogFactor);
                return half4(col, 1.0);
            }
            ENDHLSL
        }

        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode"="DepthOnly" }
            ColorMask R
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment fragDepth
            half fragDepth(Varyings i) : SV_Target { return i.positionCS.z; }
            ENDHLSL
        }

        Pass
        {
            Name "DepthNormals"
            Tags { "LightMode"="DepthNormals" }
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment fragNormals
            half4 fragNormals(Varyings i) : SV_Target { return half4(normalize(i.normalWS), 0.0); }
            ENDHLSL
        }
    }
    FallBack Off
}
