// Old-school battle swirl for the arena picture.
//
// ELI5: the picture inside the battle viewport is grabbed by the middle and twisted, then pulled
// into a black hole. Played backwards, it unwinds into the next fight.
//
// UI shader: it must honor the viewport's RectMask2D clip, so it carries the standard uGUI
// stencil block and UnityGet2DClipping.
Shader "IdleRPG/UI Whirl"
{
    Properties
    {
        [PerRendererData] _MainTex ("Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _Progress ("Progress", Range(0, 1)) = 0
        _Twist ("Twist", Float) = 28
        _Arms ("Spiral Arms", Range(1, 8)) = 3
        _Radius ("Active Radius", Range(0.1, 1.5)) = 0.85
        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        _ColorMask ("Color Mask", Float) = 15
        [Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip ("Use Alpha Clip", Float) = 0
        _ClipRect ("Clip Rect", Vector) = (-32767, -32767, 32767, 32767)
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "IgnoreProjector" = "True"
            "RenderType" = "Transparent"
            "PreviewType" = "Plane"
            "CanUseSpriteAtlas" = "True"
        }

        Stencil
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp]
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }

        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]

        Pass
        {
            Name "Whirl"

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0

            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP

            struct appdata_t
            {
                float4 vertex : POSITION;
                fixed4 color : COLOR;
                float2 texcoord : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                fixed4 color : COLOR;
                float2 texcoord : TEXCOORD0;
                float4 worldPosition : TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            sampler2D _MainTex;
            float4 _ClipRect;
            fixed4 _Color;
            float _Progress;
            float _Twist;
            float _Arms;
            float _Radius;

            v2f vert(appdata_t v)
            {
                v2f OUT;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);
                OUT.worldPosition = v.vertex;
                OUT.vertex = UnityObjectToClipPos(OUT.worldPosition);
                OUT.texcoord = v.texcoord;
                OUT.color = v.color * _Color;
                return OUT;
            }

            fixed4 frag(v2f IN) : SV_Target
            {
                float progress = saturate(_Progress);

                // Work in polar coordinates around the centre of the picture.
                float2 uv = IN.texcoord - 0.5;
                float radius = length(uv);
                float angle = atan2(uv.y, uv.x);

                // The wind varies in rings, so the picture spirals along several arms instead of one squeeze.
                float arms = 0.65 + 0.35 * sin(radius * _Arms * 3.14159265);
                angle += (_Radius - min(radius, _Radius)) * _Twist * arms * progress;
                uv = float2(cos(angle), sin(angle)) * radius;

                // Pull everything into the centre. At progress 1 no pixel is inside the picture.
                uv /= (1.0001 - progress);
                uv += 0.5;

                // The texture clamps, so far samples smear the edge inward instead of cutting the corners.
                fixed4 color = IN.color * tex2D(_MainTex, uv);

                // Drain to black only at the very end, so the swirl reads at full brightness for most of the beat.
                color.rgb *= 1.0 - smoothstep(0.85, 1.0, progress);

                #ifdef UNITY_UI_CLIP_RECT
                color.a *= UnityGet2DClipping(IN.worldPosition.xy, _ClipRect);
                #endif

                #ifdef UNITY_UI_ALPHACLIP
                clip(color.a - 0.001);
                #endif

                return color;
            }
            ENDCG
        }
    }
}
