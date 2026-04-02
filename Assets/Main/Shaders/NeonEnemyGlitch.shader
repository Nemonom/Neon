Shader "Neon/EnemyGlitch"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Color", Color) = (1,1,1,1)
        _TintColor ("Tint Color", Color) = (0,1,1,1)
        _GlitchStrength ("Glitch Strength", Range(0,1)) = 0.15
        _FlashStrength ("Flash Strength", Range(0,1)) = 0
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
            "RenderPipeline"="UniversalPipeline"
        }

        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off
        ZWrite Off

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float4 color : COLOR;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float4 color : COLOR;
                float2 uv : TEXCOORD0;
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float4 _Color;
                float4 _TintColor;
                float _GlitchStrength;
                float _FlashStrength;
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionHCS = TransformObjectToHClip(input.positionOS.xyz);
                output.color = input.color * _Color;
                output.uv = TRANSFORM_TEX(input.uv, _MainTex);
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float lineNoise = sin((_Time.y * 28.0) + input.uv.y * 80.0) * _GlitchStrength * 0.05;
                float2 glitchUv = input.uv + float2(lineNoise, 0.0);
                half4 baseCol = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, glitchUv) * input.color;
                half3 neon = lerp(baseCol.rgb, _TintColor.rgb, 0.25 + _FlashStrength * 0.5);
                neon += _TintColor.rgb * _FlashStrength * 0.8;
                return half4(neon, baseCol.a);
            }
            ENDHLSL
        }
    }
}
