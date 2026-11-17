Shader "Contra-Like/Player Color Key"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        [HideInInspector] _Color ("Tint", Color) = (1,1,1,1)
        [HideInInspector] _RendererColor ("Renderer Color", Color) = (1,1,1,1)
        [HideInInspector] _Flip ("Flip", Vector) = (1,1,1,1)
        _KeyTolerance ("Точность удаления подложки", Range(0.001,0.1)) = 0.015
        [HideInInspector] _DamageFlash ("Damage Flash", Float) = 0
        [HideInInspector] _DamageColor ("Damage Color", Color) = (1,0,0,1)
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline" }
        Blend SrcAlpha OneMinusSrcAlpha, One OneMinusSrcAlpha
        Cull Off
        ZWrite Off
        Pass
        {
            HLSLPROGRAM
            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/Core2D.hlsl"
            #pragma vertex SpriteVertex
            #pragma fragment SpriteFragment
            #pragma multi_compile_instancing
            #pragma multi_compile _ DEBUG_DISPLAY SKINNED_SPRITE
            struct Attributes { COMMON_2D_INPUTS half4 color : COLOR; UNITY_SKINNED_VERTEX_INPUTS };
            struct Varyings { COMMON_2D_OUTPUTS half4 color : COLOR; };
            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/2DCommon.hlsl"
            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
                half _KeyTolerance;
                half _DamageFlash;
                half4 _DamageColor;
            CBUFFER_END
            Varyings SpriteVertex(Attributes input)
            {
                UNITY_SKINNED_VERTEX_COMPUTE(input);
                SetUpSpriteInstanceProperties();
                input.positionOS = UnityFlipSprite(input.positionOS, unity_SpriteProps.xy);
                Varyings output = CommonUnlitVertex(input);
                output.color = input.color * _Color * unity_SpriteColor;
                return output;
            }
            half4 SpriteFragment(Varyings input) : SV_Target
            {
                half3 background = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, float2(0.001,0.999)).rgb;
                half3 pixel = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv).rgb;
                clip(distance(pixel, background) - _KeyTolerance);
                half4 color = CommonUnlitFragment(input, input.color);
                color.rgb = lerp(color.rgb, _DamageColor.rgb, saturate(_DamageFlash));
                return color;
            }
            ENDHLSL
        }
    }
}
