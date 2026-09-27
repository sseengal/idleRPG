// HD-2D lit sprite: alpha-cutout sprite that takes the URP main light, RECEIVES cast shadows and CASTS its own,
// so flat 2D art sits in a lit 3D scene instead of floating unlit on top of it.
//
// Why: URP's stock 2D sprite shaders have no ShadowCaster pass, so a SpriteRenderer using them can never cast or
// receive a shadow under this project's Universal renderer.
//
// Queue = AlphaTest (not Transparent) on purpose: cutout + ZWrite lets the parts of a multi-part character rig sort
// against each other and against 3D geometry, which a blended sprite cannot do.
//
// Note: every pass is self-contained (no HLSLINCLUDE) - ShaderLab rejects HLSLINCLUDE in this Unity version.
Shader "HD2D/SpriteLit"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        [MainColor] _BaseColor ("Tint", Color) = (1, 1, 1, 1)
        _Cutoff ("Alpha Cutoff", Range(0, 1)) = 0.5
        _LightBoost ("Direct Light Boost", Range(0, 3)) = 1
        _AmbientBoost ("Ambient Boost", Range(0, 2)) = 1
        _ShadowTint ("Shadow Tint", Color) = (0.65, 0.7, 0.85, 1)
        _ShadowStrength ("Received Shadow Strength", Range(0, 1)) = 0.65
        _Saturation ("Saturation", Range(0, 2)) = 1
    }

    SubShader
    {
        Tags
        {
            "Queue" = "AlphaTest"
            "RenderType" = "TransparentCutout"
            "IgnoreProjector" = "True"
            "RenderPipeline" = "UniversalPipeline"
            "PreviewType" = "Plane"
        }

        Pass
        {
            Name "Forward"
            Tags { "LightMode" = "UniversalForward" }

            Cull Off
            ZWrite On

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma target 3.0
            #pragma multi_compile_instancing
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile_fragment _ _SHADOWS_SOFT

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                half4 _BaseColor;
                half _Cutoff;
                half _LightBoost;
                half _AmbientBoost;
                half4 _ShadowTint;
                half _ShadowStrength;
                half _Saturation;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float2 uv         : TEXCOORD0;
                float4 color      : COLOR;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS  : SV_POSITION;
                float2 uv          : TEXCOORD0;
                float4 color       : COLOR;
                float3 normalWS    : TEXCOORD1;
                float4 shadowCoord : TEXCOORD2;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            Varyings Vert(Attributes input)
            {
                Varyings output = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);

                VertexPositionInputs positionInputs = GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs normalInputs = GetVertexNormalInputs(input.normalOS);

                output.positionCS = positionInputs.positionCS;
                output.normalWS = normalInputs.normalWS;
                output.uv = TRANSFORM_TEX(input.uv, _MainTex);
                output.color = input.color;
                output.shadowCoord = GetShadowCoord(positionInputs);
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);

                half4 tex = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);
                half4 albedo = tex * input.color * _BaseColor;
                clip(albedo.a - _Cutoff);

                float3 normalWS = normalize(input.normalWS);
                Light mainLight = GetMainLight(input.shadowCoord);

                half shadowAtten = lerp(1.0h, mainLight.shadowAttenuation, _ShadowStrength);
                half ndotl = saturate(dot(normalWS, mainLight.direction));
                half3 direct = mainLight.color * (ndotl * shadowAtten) * _LightBoost;
                half3 ambient = SampleSH(normalWS) * _AmbientBoost * _ShadowTint.rgb;

                half3 lit = albedo.rgb * (direct + ambient);
                half luminance = dot(lit, half3(0.2126h, 0.7152h, 0.0722h));
                lit = lerp(luminance.xxx, lit, _Saturation);

                return half4(lit, 1.0h);
            }
            ENDHLSL
        }

        // Without this pass the sprite can never cast a shadow, whatever shadowCastingMode says.
        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }

            ZWrite On
            ZTest LEqual
            ColorMask 0
            Cull Off

            HLSLPROGRAM
            #pragma vertex ShadowVert
            #pragma fragment ShadowFrag
            #pragma target 3.0
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                half4 _BaseColor;
                half _Cutoff;
                half _LightBoost;
                half _AmbientBoost;
                half4 _ShadowTint;
                half _ShadowStrength;
                half _Saturation;
            CBUFFER_END

            // Set by URP while it renders the shadow map (one value per cascade).
            float3 _LightDirection;

            struct ShadowAttributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float2 uv         : TEXCOORD0;
                float4 color      : COLOR;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct ShadowVaryings
            {
                float4 positionCS : SV_POSITION;
                float2 uv         : TEXCOORD0;
                float4 color      : COLOR;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            ShadowVaryings ShadowVert(ShadowAttributes input)
            {
                ShadowVaryings output = (ShadowVaryings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);

                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                float3 normalWS = TransformObjectToWorldNormal(input.normalOS);
                float4 positionCS = TransformWorldToHClip(ApplyShadowBias(positionWS, normalWS, _LightDirection));

                #if UNITY_REVERSED_Z
                    positionCS.z = min(positionCS.z, UNITY_NEAR_CLIP_VALUE);
                #else
                    positionCS.z = max(positionCS.z, UNITY_NEAR_CLIP_VALUE);
                #endif

                output.positionCS = positionCS;
                output.uv = TRANSFORM_TEX(input.uv, _MainTex);
                output.color = input.color;
                return output;
            }

            half4 ShadowFrag(ShadowVaryings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                half4 tex = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);
                half alpha = tex.a * input.color.a * _BaseColor.a;
                clip(alpha - _Cutoff);
                return 0;
            }
            ENDHLSL
        }

        // Keeps the sprite in the depth texture, which depth of field and SSAO read.
        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }

            ZWrite On
            ColorMask R
            Cull Off

            HLSLPROGRAM
            #pragma vertex DepthVert
            #pragma fragment DepthFrag
            #pragma target 3.0
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                half4 _BaseColor;
                half _Cutoff;
                half _LightBoost;
                half _AmbientBoost;
                half4 _ShadowTint;
                half _ShadowStrength;
                half _Saturation;
            CBUFFER_END

            struct DepthAttributes
            {
                float4 positionOS : POSITION;
                float2 uv         : TEXCOORD0;
                float4 color      : COLOR;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct DepthVaryings
            {
                float4 positionCS : SV_POSITION;
                float2 uv         : TEXCOORD0;
                float4 color      : COLOR;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            DepthVaryings DepthVert(DepthAttributes input)
            {
                DepthVaryings output = (DepthVaryings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = TRANSFORM_TEX(input.uv, _MainTex);
                output.color = input.color;
                return output;
            }

            half4 DepthFrag(DepthVaryings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                half4 tex = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);
                half alpha = tex.a * input.color.a * _BaseColor.a;
                clip(alpha - _Cutoff);
                return 0;
            }
            ENDHLSL
        }
    }

    FallBack "Universal Render Pipeline/Unlit"
}
