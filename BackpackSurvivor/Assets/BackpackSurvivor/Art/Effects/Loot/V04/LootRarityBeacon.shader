Shader "BackpackSurvivor/Effects/Loot Rarity Beacon"
{
    Properties
    {
        [MainColor] _BaseColor("Equipment rarity colour", Color) = (1,1,1,1)
        _BeamHeight("Beam height (m)", Range(0.5,4)) = 1.6
        _BeamWidth("Beam width (m)", Range(0.05,1)) = 0.22
        _BaseRadius("Ground marker radius (m)", Range(0.1,1)) = 0.32
        _Opacity("Visual intensity", Range(0,0.6)) = 0.14
        _GroundHeight("Map floor (world m)", Float) = 0
        _FadeStart("Camera distance fade start (m)", Float) = 45
        _FadeEnd("Camera distance fade end (m)", Float) = 65
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }
        Pass
        {
            Name "EquipmentBeacon"
            Tags { "LightMode"="UniversalForward" }
            Blend SrcAlpha One
            Cull Off
            ZWrite Off
            ZTest LEqual

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_instancing
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Fog.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
                float _BeamHeight, _BeamWidth, _BaseRadius, _Opacity;
                float _GroundHeight, _FadeStart, _FadeEnd;
            CBUFFER_END
            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 uv : TEXCOORD0; // Z = 0 upright quad; Z = 1 ground quad.
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 uv : TEXCOORD0;
                half2 fades : TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };
            Varyings Vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                float3 anchor = TransformObjectToWorld(float3(0,0,0));
                anchor.y = _GroundHeight + 0.025;
                float3 towardCamera = _WorldSpaceCameraPos - anchor;
                float3 right = float3(towardCamera.z,0,-towardCamera.x);
                right *= rsqrt(max(dot(right,right),0.0001));
                float3 beam = anchor + right * input.positionOS.x * _BeamWidth * (1.0-input.uv.y*0.25);
                beam.y += input.uv.y * _BeamHeight;
                float3 marker = anchor + float3(input.positionOS.x,0,input.positionOS.z) * (_BaseRadius*2.0);
                float3 world = lerp(beam,marker,input.uv.z);
                output.positionCS = TransformWorldToHClip(world);
                output.uv = input.uv;
                output.fades.x = saturate((_FadeEnd-length(towardCamera))/max(_FadeEnd-_FadeStart,0.01));
                output.fades.y = ComputeFogFactor(output.positionCS.z);
                return output;
            }
            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                half across = abs(input.uv.x-0.5h)*2.0h;
                half core = exp2(-across*10.0h);
                half soft = saturate(1.0h-across);
                half height = saturate(1.0h-input.uv.y);
                half beam = (soft*soft*0.32h+core*0.68h)*height*height*smoothstep(0.0h,0.045h,input.uv.y);
                half radius = length(input.uv.xy-0.5h)*2.0h;
                half ring = smoothstep(0.50h,0.65h,radius)*(1.0h-smoothstep(0.78h,0.98h,radius));
                half disc = (1.0h-smoothstep(0.0h,0.95h,radius))*0.12h;
                half shape = lerp(beam,ring*0.65h+disc,input.uv.z);
                half opacity = saturate(shape*_Opacity*_BaseColor.a*input.fades.x);
                // Additive fog must approach black, never paint a fog-coloured rectangle.
                half3 colour = MixFogColor(_BaseColor.rgb,half3(0,0,0),input.fades.y);
                return half4(colour,opacity);
            }
            ENDHLSL
        }
    }
}
