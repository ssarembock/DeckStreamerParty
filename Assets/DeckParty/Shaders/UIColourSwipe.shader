// Deck Party: a colour swipe across a UI rectangle with rounded corners. A band of colour crosses the rect (Band), or the
// colour sweeps across and fills it, holds, then fades (Wipe). Drawn by the ColourSwipe component, which sets these
// properties from its inspector. Sizes are in the rect's own units (card pixels). Works inside Masks and RectMask2D like
// the default UI shader.
//
// Timing: when _Driven is 0 the shader loops on its own clock (_Time, so it also previews in the editor); when it is 1
// the component drives _LocalTime (seconds since the swipe started; below 0 = nothing shows).
Shader "DeckParty/UI/Colour Swipe"
{
    Properties
    {
        [PerRendererData] _MainTex ("Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)

        [HDR] _SwipeColor ("Colour", Color) = (1, 1, 1, 0.75)
        [HDR] _SwipeColor2 ("Second Colour", Color) = (0.54, 0.31, 1, 0.75)
        _Gradient ("Use Second Colour (0 off, 1 on)", Range(0, 1)) = 0
        _RectSize ("Rect Size (set by the component)", Vector) = (180, 252, 0, 0)
        _Inset ("Inset", Float) = 3
        _Radius ("Corner Radius", Float) = 12
        _Angle ("Angle (degrees)", Float) = 30
        _BandWidth ("Band Width (fraction of the card)", Range(0.01, 1)) = 0.25
        _Softness ("Softness", Range(0, 1)) = 0.6
        _Style ("Style (0 band, 1 wipe)", Float) = 0
        _Additive ("Additive (0 paint over, 1 add light)", Range(0, 1)) = 1
        _Ease ("Ease In and Out (0 off, 1 on)", Range(0, 1)) = 1
        _SwipeTime ("Swipe Time", Float) = 0.8
        _RepeatDelay ("Repeat Delay", Float) = 2
        _Hold ("Wipe Hold", Float) = 0.3
        _FadeOut ("Wipe Fade Out", Float) = 0.4
        _Offset ("Clock Offset (set by the component)", Float) = 0
        _Driven ("Driven by the component (0 loop, 1 driven)", Float) = 0
        _LocalTime ("Local Time (set by the component)", Float) = -1
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
        // Premultiplied output: alpha 0 adds light, alpha 1 paints over.
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

            half4 _SwipeColor;
            half4 _SwipeColor2;
            float _Gradient;
            float4 _RectSize;
            float _Inset;
            float _Radius;
            float _Angle;
            float _BandWidth;
            float _Softness;
            float _Style;
            float _Additive;
            float _Ease;
            float _SwipeTime;
            float _RepeatDelay;
            float _Hold;
            float _FadeOut;
            float _Offset;
            float _Driven;
            float _LocalTime;
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

                // Clip to the rounded rectangle (Inset in from the edge, Corner Radius) so the swipe stays on the card.
                float2 halfSize = max(_RectSize.xy * 0.5 - _Inset, 0.001);
                float radius = clamp(_Radius - _Inset, 0.0, min(halfSize.x, halfSize.y));
                float2 q = abs(p) - (halfSize - radius);
                float d = length(max(q, 0.0)) + min(max(q.x, q.y), 0.0) - radius;
                float aa = max(fwidth(d), 1e-4);
                float inside = saturate(-d / aa + 0.5);

                // Seconds into the current swipe.
                float swipe = max(_SwipeTime, 0.001);
                float wipe = step(0.5, _Style);
                float active = swipe + wipe * (max(_Hold, 0.0) + max(_FadeOut, 0.0));
                float cycle = max(active + max(_RepeatDelay, 0.0), 0.001);
                float looped = fmod(_Time.y + _Offset, cycle);
                float local = lerp(looped, _LocalTime, step(0.5, _Driven));
                float playing = step(0.0, local);

                float t = saturate(local / swipe);
                t = lerp(t, t * t * (3.0 - 2.0 * t), step(0.5, _Ease));

                // Position across the rect along the swipe's direction: 0 where it starts, 1 where it ends.
                float angle = radians(_Angle);
                float2 dir = float2(cos(angle), sin(angle));
                float2 halfRect = max(_RectSize.xy * 0.5, 0.001);
                float extent = max(abs(dir.x) * halfRect.x + abs(dir.y) * halfRect.y, 0.001);
                float s = (dot(p, dir) + extent) / (2.0 * extent);

                float w = max(_BandWidth, 0.001);

                // Band: a stripe that crosses from before the start to past the end.
                float centre = lerp(-w * 0.5, 1.0 + w * 0.5, t);
                float fromCentre = abs(s - centre);
                float bandAlpha = 1.0 - smoothstep(w * 0.5 * (1.0 - saturate(_Softness)), w * 0.5, fromCentre);
                float across = saturate((s - (centre - w * 0.5)) / w);

                // Wipe: the colour fills everything behind a soft front, holds, then fades out.
                float front = lerp(0.0, 1.0 + w, t);
                float wipeAlpha = saturate((front - s) / w);
                wipeAlpha *= 1.0 - saturate((local - swipe - max(_Hold, 0.0)) / max(_FadeOut, 0.001));

                float alpha = lerp(bandAlpha, wipeAlpha, wipe);
                // Second colour: the band's trailing edge (or, for a wipe, the side it starts from).
                float mixAt = lerp(across, s, wipe);
                half4 colour = lerp(_SwipeColor2, _SwipeColor, lerp(1.0, mixAt, saturate(_Gradient)));

                half a = saturate(alpha) * inside * playing * _Strength * colour.a * IN.color.a;
                half3 rgb = colour.rgb * IN.color.rgb;

                #ifdef UNITY_UI_CLIP_RECT
                half2 m = saturate((_ClipRect.zw - _ClipRect.xy - abs(IN.mask.xy)) * IN.mask.zw);
                a *= m.x * m.y;
                #endif

                #ifdef UNITY_UI_ALPHACLIP
                clip(a - 0.001);
                #endif

                return half4(rgb * a, a * (1.0 - saturate(_Additive)));
            }
        ENDCG
        }
    }
}
