Shader "BackpackSurvivor/Environment/Recovery Ground"
{
    Properties
    {
        [MainColor] _BaseColor("Base tone", Color) = (0.25, 0.28, 0.27, 1)
        _SecondaryColor("Aggregate tone", Color) = (0.32, 0.34, 0.31, 1)
        [NoScaleOffset] _SurfaceMask("Surface: R aggregate / G cracks / B wet / A grime", 2D) = "gray" {}
        [NoScaleOffset] _NormalDetail("Linear normal: R world X / G world Z", 2D) = "gray" {}
        _MetersPerTile("World metres per detail tile", Range(0.25, 16)) = 4
        _MacroScale("Macro UV scale", Range(0.01, 0.5)) = 0.13
        _BumpScale("Grain normal strength", Range(0, 1)) = 0.2
        _Smoothness("Dry smoothness", Range(0, 0.8)) = 0.22
        _Wetness("Wet patch strength", Range(0, 1)) = 0.45
        _CrackStrength("Crack contrast", Range(0, 1)) = 0.45
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque"
            "RenderPipeline" = "UniversalPipeline"
            "UniversalMaterialType" = "Lit"
            "Queue" = "Geometry"
            "IgnoreProjector" = "True"
        }
        LOD 250
        Cull Back
        ZWrite On
        ZTest LEqual

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

        // Keep exactly the same material buffer in every pass for the SRP Batcher.
        CBUFFER_START(UnityPerMaterial)
            half4 _BaseColor;
            half4 _SecondaryColor;
            float _MetersPerTile;
            float _MacroScale;
            half _BumpScale;
            half _Smoothness;
            half _Wetness;
            half _CrackStrength;
        CBUFFER_END

        TEXTURE2D(_SurfaceMask);
        SAMPLER(sampler_SurfaceMask);
        TEXTURE2D(_NormalDetail);
        SAMPLER(sampler_NormalDetail);

        float2 RecoveryDetailUV(float3 positionWS)
        {
            // Object scale and rotation cannot stretch the grain or break adjacent tiles.
            return GetAbsolutePositionWS(positionWS).xz / max(_MetersPerTile, 0.01);
        }

        float2 RecoveryMacroUV(float2 detailUV)
        {
            // A fixed rotation decorrelates the broad stains from the repeated fine grain.
            return float2(detailUV.x * 0.8 - detailUV.y * 0.6,
                          detailUV.x * 0.6 + detailUV.y * 0.8) * _MacroScale + float2(0.173, 0.417);
        }

        half RecoveryWetAmount(half4 mask, half4 macroMask)
        {
            return saturate(_Wetness * smoothstep(0.45h, 0.85h, macroMask.b)
                            * lerp(0.85h, 1.0h, mask.b));
        }

        half3 RecoveryNormalWS(float2 detailUV, half3 geometricNormalWS, half wet)
        {
            // This is an ordinary linear RG texture, not a platform-swizzled Unity normal map.
            half2 normalXY = SAMPLE_TEXTURE2D(_NormalDetail, sampler_NormalDetail, detailUV).rg * 2.0h - 1.0h;
            normalXY *= _BumpScale * lerp(1.0h, 0.5h, wet);
            half normalZ = sqrt(saturate(1.0h - dot(normalXY, normalXY)));
            half3 baseNormal = normalize(geometricNormalWS);
            half3 detailWS = half3(normalXY.x, 0, normalXY.y);
            // Project onto the geometric tangent plane; vertical slab edges retain clean normals.
            detailWS -= baseNormal * dot(detailWS, baseNormal);
            half horizontalWeight = abs(baseNormal.y);
            return normalize(baseNormal * max(normalZ, 0.01h) + detailWS * horizontalWeight);
        }
        ENDHLSL

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForwardOnly" }

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex RecoveryGroundVertex
            #pragma fragment RecoveryGroundFragment
            #pragma multi_compile_instancing
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            #pragma multi_compile _ _CLUSTER_LIGHT_LOOP
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Fog.hlsl"

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct RecoveryAttributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct RecoveryVaryings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                half3 normalWS : TEXCOORD1;
                half4 fogAndVertexLight : TEXCOORD2;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            RecoveryVaryings RecoveryGroundVertex(RecoveryAttributes input)
            {
                RecoveryVaryings output = (RecoveryVaryings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                VertexPositionInputs positions = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionCS = positions.positionCS;
                output.positionWS = positions.positionWS;
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.fogAndVertexLight.x = ComputeFogFactor(positions.positionCS.z);
                #if defined(_ADDITIONAL_LIGHTS_VERTEX)
                    output.fogAndVertexLight.yzw = VertexLighting(positions.positionWS, output.normalWS);
                #endif
                return output;
            }

            half4 RecoveryGroundFragment(RecoveryVaryings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                float2 detailUV = RecoveryDetailUV(input.positionWS);
                half4 mask = SAMPLE_TEXTURE2D(_SurfaceMask, sampler_SurfaceMask, detailUV);
                half4 macroMask = SAMPLE_TEXTURE2D(_SurfaceMask, sampler_SurfaceMask, RecoveryMacroUV(detailUV));
                half wet = RecoveryWetAmount(mask, macroMask);
                half crack = saturate(mask.g * _CrackStrength);

                SurfaceData surface = (SurfaceData)0;
                surface.albedo = lerp(_BaseColor.rgb, _SecondaryColor.rgb, mask.r);
                surface.albedo *= lerp(0.88h, 1.12h, macroMask.a);
                surface.albedo *= (1.0h - crack * 0.65h) * lerp(1.0h, 0.65h, wet);
                surface.metallic = 0;
                surface.specular = half3(0, 0, 0);
                surface.smoothness = lerp(_Smoothness * lerp(0.78h, 1.05h, mask.r), 0.82h, wet);
                surface.occlusion = 1.0h - crack * 0.22h;
                surface.normalTS = half3(0, 0, 1);
                surface.alpha = 1;

                InputData lighting = (InputData)0;
                lighting.positionWS = input.positionWS;
                lighting.positionCS = input.positionCS;
                lighting.normalWS = RecoveryNormalWS(detailUV, input.normalWS, wet);
                lighting.viewDirectionWS = GetWorldSpaceNormalizeViewDir(input.positionWS);
                lighting.shadowCoord = TransformWorldToShadowCoord(input.positionWS);
                lighting.fogCoord = InitializeInputDataFog(float4(input.positionWS, 1), input.fogAndVertexLight.x);
                lighting.vertexLighting = input.fogAndVertexLight.yzw;
                lighting.bakedGI = SampleSHPixel(half3(0, 0, 0), lighting.normalWS);
                lighting.shadowMask = half4(1, 1, 1, 1);
                lighting.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(input.positionCS);

                half4 color = UniversalFragmentPBR(lighting, surface);
                color.rgb = MixFog(color.rgb, lighting.fogCoord);
                color.a = 1;
                return color;
            }
            ENDHLSL
        }

        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }
            ColorMask 0

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex ShadowPassVertex
            #pragma fragment ShadowPassFragment
            #pragma multi_compile_instancing
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW
            #include "Packages/com.unity.render-pipelines.universal/Shaders/ShadowCasterPass.hlsl"
            ENDHLSL
        }

        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }
            ColorMask R

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex DepthOnlyVertex
            #pragma fragment DepthOnlyFragment
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/Shaders/DepthOnlyPass.hlsl"
            ENDHLSL
        }

        Pass
        {
            Name "DepthNormals"
            Tags { "LightMode" = "DepthNormalsOnly" }

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex RecoveryNormalsVertex
            #pragma fragment RecoveryNormalsFragment
            #pragma multi_compile_instancing
            #pragma multi_compile_fragment _ _GBUFFER_NORMALS_OCT
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Packing.hlsl"

            struct RecoveryNormalsAttributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct RecoveryNormalsVaryings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                half3 normalWS : TEXCOORD1;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            RecoveryNormalsVaryings RecoveryNormalsVertex(RecoveryNormalsAttributes input)
            {
                RecoveryNormalsVaryings output = (RecoveryNormalsVaryings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                VertexPositionInputs positions = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionCS = positions.positionCS;
                output.positionWS = positions.positionWS;
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                return output;
            }

            half4 RecoveryNormalsFragment(RecoveryNormalsVaryings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                float2 detailUV = RecoveryDetailUV(input.positionWS);
                half4 mask = SAMPLE_TEXTURE2D(_SurfaceMask, sampler_SurfaceMask, detailUV);
                half4 macroMask = SAMPLE_TEXTURE2D(_SurfaceMask, sampler_SurfaceMask, RecoveryMacroUV(detailUV));
                half3 normalWS = RecoveryNormalWS(detailUV, input.normalWS, RecoveryWetAmount(mask, macroMask));
                #if defined(_GBUFFER_NORMALS_OCT)
                    float2 oct = PackNormalOctQuadEncode(normalWS);
                    return half4(PackFloat2To888(saturate(oct * 0.5 + 0.5)), 0);
                #else
                    return half4(normalWS, 0);
                #endif
            }
            ENDHLSL
        }
    }
    Fallback "Hidden/Universal Render Pipeline/FallbackError"
}
