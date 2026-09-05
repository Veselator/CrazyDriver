// An additive, unlit beam. Brightness and opacity are separate knobs on purpose: intensity drives
// how hard the beam blooms, alpha drives how present it is at all, and an activation flicker wants
// to move them independently.
Shader "CrazyDriver/Laser Beam"
{
    Properties
    {
        [HDR] _BaseColor ("Colour", Color) = (1, 0.18, 0.12, 1)
        _Intensity ("Intensity", Range(0, 30)) = 5
        _Alpha ("Alpha", Range(0, 1)) = 1
        _CoreSharpness ("Core Sharpness", Range(0.25, 8)) = 2
        _EdgeBoost ("Edge Boost", Range(0, 1)) = 0.25
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Transparent"
            "Queue" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
            "IgnoreProjector" = "True"
        }

        Pass
        {
            Name "LaserBeam"
            Tags { "LightMode" = "UniversalForward" }

            // Additive rather than alpha blend: a laser adds light to whatever is behind it instead
            // of replacing it, which is what makes it read as glowing rather than as coloured glass.
            // SrcAlpha as the source factor keeps _Alpha as a usable fade.
            Blend SrcAlpha One
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 normalWS   : TEXCOORD0;
                float3 viewDirWS  : TEXCOORD1;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
                half _Intensity;
                half _Alpha;
                half _CoreSharpness;
                half _EdgeBoost;
            CBUFFER_END

            Varyings Vert(Attributes input)
            {
                Varyings output = (Varyings)0;

                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);

                VertexPositionInputs positions = GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs normals = GetVertexNormalInputs(input.normalOS);

                output.positionCS = positions.positionCS;
                output.normalWS = normals.normalWS;
                output.viewDirWS = GetWorldSpaceViewDir(positions.positionWS);

                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);

                float3 normalWS = normalize(input.normalWS);
                float3 viewDirWS = normalize(input.viewDirWS);

                // The part of the cylinder facing the camera is the beam's core; surfaces seen at a
                // grazing angle are its edge. Without this the beam is a flat-shaded tube and reads
                // as a plastic rod rather than as light.
                half facing = saturate(abs(dot(normalWS, viewDirWS)));
                half core = pow(facing, _CoreSharpness);

                // Edge boost keeps the silhouette from vanishing entirely at a sharp core falloff.
                half profile = core + _EdgeBoost * (1.0h - core);

                half3 colour = _BaseColor.rgb * _Intensity * profile;
                half alpha = saturate(_BaseColor.a * _Alpha * profile);

                return half4(colour, alpha);
            }
            ENDHLSL
        }
    }

    Fallback Off
}
