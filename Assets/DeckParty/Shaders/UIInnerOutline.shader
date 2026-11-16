// Deck Party: an inner outline (and optional inner glow) along the edge of a UI rectangle with rounded corners.
// Drawn by the InnerOutline component, which covers the card (or any rect) and sets these properties from its
// inspector. Sizes are in the rect's own units (card pixels), so the line looks the same however the card is scaled.
// Works inside Masks and RectMask2D like the default UI shader.
Shader "DeckParty/UI/Inner Outline"
{
    Properties
    {
        [PerRendererData] _MainTex ("Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)

        [HDR] _OutlineColor ("Outline Colour", Color) = (0.54, 0.31, 1, 1)
        _RectSize ("Rect Size (set by the component)", Vector) = (180, 252, 0, 0)
        _Width ("Width", Float) = 4
        _Inset ("Inset", Float) = 3
        _Radius ("Corner Radius", Float) = 12
        _Softness ("Softness", Range(0, 1)) = 0.4
        _GlowWidth ("Glow Width", Float) = 14
        _GlowStrength ("Glow Strength", Range(0, 1)) = 0.35
        _PulseSpeed ("Pulse Speed", Float) = 0.8
        _PulseAmount ("Pulse Amount", Range(0, 1)) = 0.35
        [HDR] _OutlineColor2 ("Second Colour", Color) = (0.2, 0.9, 1, 1)
        _ColorMix ("Rotating Colours (0 off, 1 on)", Range(0, 1)) = 1
        _RotationSpeed ("Rotation Speed (turns per second)", Float) = 0.25
        _ColorRepeats ("Colour Repeats", Float) = 2
        _ColorSharpness ("Colour Sharpness", Range(0, 1)) = 0.2
        _BorderSize ("Border Size (0: no limit)", Float) = 18
        _BorderFade ("Border Fade", Float) = 6
        _Strength ("Strength (fade, set by the component)", Range(0, 1)) = 1

        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        _ColorMask ("Color Mask", Float) = 15
        [Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip ("Use Alpha Clip", Float) = 0
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
        Blend One OneMinusSrcAlpha
        ColorMask [_ColorMask]

        Pass
        {
            Name "Default"
        CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0

            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP

            struct appdata_t
            {
                float4 vertex   : POSITION;
                float4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex        : SV_POSITION;
                fixed4 color         : COLOR;
                float2 texcoord      : TEXCOORD0;
                float4 worldPosition : TEXCOORD1;
                float4 mask          : TEXCOORD2;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            sampler2D _MainTex;
            fixed4 _Color;
            fixed4 _TextureSampleAdd;
            float4 _ClipRect;
            float4 _MainTex_ST;
            float _UIMaskSoftnessX;
            float _UIMaskSoftnessY;

            half4 _OutlineColor;
            float4 _RectSize;
            float _Width;
            float _Inset;
            float _Radius;
            float _Softness;
            float _GlowWidth;
            float _GlowStrength;
            float _PulseSpeed;
            float _PulseAmount;
            half4 _OutlineColor2;
            float _ColorMix;
            float _RotationSpeed;
            float _ColorRepeats;
            float _ColorSharpness;
            float _BorderSize;
            float _BorderFade;
            float _Strength;

            v2f vert(appdata_t v)
            {
                v2f OUT;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);
                float4 vPosition = UnityObjectToClipPos(v.vertex);
                OUT.worldPosition = v.vertex;
                OUT.vertex = vPosition;

                float2 pixelSize = vPosition.w;
                pixelSize /= float2(1, 1) * abs(mul((float2x2)UNITY_MATRIX_P, _ScreenParams.xy));
                float4 clampedRect = clamp(_ClipRect, -2e10, 2e10);
                OUT.mask = float4(v.vertex.xy * 2 - clampedRect.xy - clampedRect.zw,
                                  0.25 / (0.25 * half2(_UIMaskSoftnessX, _UIMaskSoftnessY) + abs(pixelSize.xy)));

                // The component's quad has uv (0,0) to (1,1) across its rect.
                OUT.texcoord = v.texcoord;
                OUT.color = v.color * _Color;
                return OUT;
            }

            fixed4 frag(v2f IN) : SV_Target
            {
                // Where this pixel is on the rect, in the rect's units, from its centre.
                float2 p = (IN.texcoord - 0.5) * _RectSize.xy;
                float2 halfSize = max(_RectSize.xy * 0.5 - _Inset, 0.001);
                float radius = clamp(_Radius - _Inset, 0.0, min(halfSize.x, halfSize.y));
                float2 q = abs(p) - (halfSize - radius);

                // Signed distance to the rounded rectangle's edge (negative inside); depth = how far inside the edge.
                float d = length(max(q, 0.0)) + min(max(q.x, q.y), 0.0) - radius;
                float depth = -d;
                float aa = max(fwidth(d), 1e-4);
                float inside = saturate(depth / aa + 0.5);

                // The line: solid from the edge inward, fading out over the last part of its width (Softness).
                float width = max(_Width, 0.0);
                float feather = max(_Softness * width, aa);
                float band = inside * (1.0 - saturate((depth - (width - feather)) / feather));
                band *= step(0.0001, width);

                // The glow: a soft light that fades further into the card.
                float glow = inside * _GlowStrength * exp(-max(depth, 0.0) / max(_GlowWidth * 0.5, 0.001));
                glow *= step(0.0001, _GlowWidth);

                // A slow breathing pulse.
                float pulse = 1.0 - _PulseAmount * (0.5 + 0.5 * sin(_Time.y * _PulseSpeed * 6.2831853));

                // Rotating colours: a wave between the two colours that travels round the edge. The angle is taken on the
                // rect squashed to a circle, so the wave moves evenly along the long and the short sides.
                float2 n = p / max(_RectSize.xy * 0.5, 0.001);
                float around = atan2(n.y, n.x) / 6.2831853;
                float repeats = max(round(_ColorRepeats), 1.0);
                float wave = 0.5 + 0.5 * cos((around * repeats - _Time.y * _RotationSpeed) * 6.2831853);
                // Sharpness narrows the blend between the colours (0: one long smooth fade, 1: almost hard bands).
                float edge = 0.5 * (1.0 - 0.9 * saturate(_ColorSharpness));
                wave = smoothstep(0.5 - edge, 0.5 + edge, wave);
                half4 colour = lerp(_OutlineColor, _OutlineColor2, wave * saturate(_ColorMix));

                // The border: nothing deeper than Border Size from the edge (the centre stays clear), with a soft edge.
                float borderFade = max(_BorderFade, aa);
                float border = 1.0 - saturate((depth - (_BorderSize - borderFade)) / borderFade);
                border = lerp(1.0, border, step(0.0001, _BorderSize));

                half alpha = saturate(max(band, glow)) * border * pulse * _Strength * colour.a * IN.color.a;
                half3 rgb = colour.rgb * IN.color.rgb;

                #ifdef UNITY_UI_CLIP_RECT
                half2 m = saturate((_ClipRect.zw - _ClipRect.xy - abs(IN.mask.xy)) * IN.mask.zw);
                alpha *= m.x * m.y;
                #endif

                #ifdef UNITY_UI_ALPHACLIP
                clip(alpha - 0.001);
                #endif

                return half4(rgb * alpha, alpha);
            }
        ENDCG
        }
    }
}
