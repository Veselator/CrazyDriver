// Lit surface that can be flashed towards a flat colour on hit.
//
// The flash is a blend towards _FlashColor rather than added emission: added light on a bright
// texture barely reads, while a blend replaces the surface and is unmistakable at any base colour.
// _FlashBrightness then scales that colour past 1 so the flash can bloom instead of just going pale.
Shader "CrazyDriver/Flash Lit"
{
    Properties
    {
        _BaseMap ("Base Map", 2D) = "white" {}
        _BaseColor ("Base Colour", Color) = (1, 1, 1, 1)

        [HDR] _FlashColor ("Flash Colour", Color) = (1, 1, 1, 1)
        _FlashAmount ("Flash Amount", Range(0, 1)) = 0
        _FlashBrightness ("Flash Brightness", Range(1, 8)) = 2

        _AmbientBoost ("Ambient Boost", Range(0, 1)) = 0.35
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque"
            "Queue" = "Geometry"
            "RenderPipeline" = "UniversalPipeline"
        }

        Pass
        {
            Name "FlashForward"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS_CASCADE

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float2 uv         : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv         : TEXCOORD0;
                float3 normalWS   : TEXCOORD1;
                float3 positionWS : TEXCOORD2;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            // Per-instance so a MaterialPropertyBlock can flash one enemy without cloning the
            // material or disturbing the others sharing it.
            UNITY_INSTANCING_BUFFER_START(Props)
                UNITY_DEFINE_INSTANCED_PROP(float4, _BaseColor)
                UNITY_DEFINE_INSTANCED_PROP(float4, _FlashColor)
                UNITY_DEFINE_INSTANCED_PROP(float, _FlashAmount)
                UNITY_DEFINE_INSTANCED_PROP(float, _FlashBrightness)
                UNITY_DEFINE_INSTANCED_PROP(float, _AmbientBoost)
            UNITY_INSTANCING_BUFFER_END(Props)

            float4 _BaseMap_ST;

            Varyings Vert(Attributes input)
            {
                Varyings output = (Varyings)0;

                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);

                VertexPositionInputs positions = GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs normals = GetVertexNormalInputs(input.normalOS);

                output.positionCS = positions.positionCS;
                output.positionWS = positions.positionWS;
                output.normalWS = normals.normalWS;
                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);

                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);

                half4 baseColor = UNITY_ACCESS_INSTANCED_PROP(Props, _BaseColor);
                half4 flashColor = UNITY_ACCESS_INSTANCED_PROP(Props, _FlashColor);
                half flashAmount = UNITY_ACCESS_INSTANCED_PROP(Props, _FlashAmount);
                half flashBrightness = UNITY_ACCESS_INSTANCED_PROP(Props, _FlashBrightness);
                half ambientBoost = UNITY_ACCESS_INSTANCED_PROP(Props, _AmbientBoost);

                half3 albedo = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv).rgb * baseColor.rgb;

                Light mainLight = GetMainLight();
                half ndotl = saturate(dot(normalize(input.normalWS), mainLight.direction));

                // Half-lambert lifted by an ambient term: the art is flat shaded, and a hard
                // terminator on it looks like a bug rather than like lighting.
                half diffuse = lerp(ambientBoost, 1.0h, ndotl * 0.5h + 0.5h);
                half3 lit = albedo * mainLight.color * diffuse;

                // The blend happens after lighting, so a flashed enemy is uniformly bright rather
                // than still carrying the shading of whichever way it happened to be facing.
                half3 flashed = flashColor.rgb * flashBrightness;
                half3 result = lerp(lit, flashed, saturate(flashAmount));

                return half4(result, 1.0h);
            }
            ENDHLSL
        }

        // Enemies cast shadows onto the road; without this pass they would float.
        UsePass "Universal Render Pipeline/Lit/ShadowCaster"
    }

    Fallback "Universal Render Pipeline/Lit"
}
