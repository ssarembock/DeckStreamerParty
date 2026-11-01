using System;
using UnityEngine;

namespace DeckParty
{
    /// <summary>Procedurally generated shapes used by placeholders and effects before real art is assigned.</summary>
    public static class RuntimeSprites
    {
        static Sprite s_Circle;
        static Sprite s_Glow;
        static Sprite s_Rounded;
        static Sprite s_Square;

        public static Sprite Circle => s_Circle != null ? s_Circle : (s_Circle = Build("dp_circle", 128, Vector4.zero, (x, y) =>
        {
            float d = Mathf.Sqrt(x * x + y * y);
            return Mathf.Clamp01((1f - d) * 64f);
        }));

        public static Sprite Glow => s_Glow != null ? s_Glow : (s_Glow = Build("dp_glow", 128, Vector4.zero, (x, y) =>
        {
            float d = Mathf.Sqrt(x * x + y * y);
            float a = Mathf.Clamp01(1f - d);
            return a * a;
        }));

        public static Sprite Rounded => s_Rounded != null ? s_Rounded : (s_Rounded = Build("dp_rounded", 64, new Vector4(20, 20, 20, 20), (x, y) =>
        {
            // Rounded box SDF in a -1..1 space with corner radius 0.55.
            const float r = 0.55f;
            float qx = Mathf.Abs(x) - (1f - r);
            float qy = Mathf.Abs(y) - (1f - r);
            float outside = new Vector2(Mathf.Max(qx, 0f), Mathf.Max(qy, 0f)).magnitude + Mathf.Min(Mathf.Max(qx, qy), 0f) - r;
            return Mathf.Clamp01(-outside * 32f);
        }));

        public static Sprite Square => s_Square != null ? s_Square : (s_Square = Build("dp_square", 8, Vector4.zero, (x, y) => 1f));

        static Sprite s_Vignette;

        /// <summary>Transparent centre fading to opaque edges (the danger vignette).</summary>
        public static Sprite Vignette => s_Vignette != null ? s_Vignette : (s_Vignette = Build("dp_vignette", 128, Vector4.zero, (x, y) =>
        {
            float edge = Mathf.Max(Mathf.Abs(x), Mathf.Abs(y));
            float t = Mathf.Clamp01((edge - 0.55f) / 0.45f);
            return t * t;
        }));

        static Sprite s_Arrow;

        /// <summary>A soft horizontal bar with a pointed right end (targeting arrows and link lines).</summary>
        public static Sprite Arrow => s_Arrow != null ? s_Arrow : (s_Arrow = Build("dp_arrow", 64, Vector4.zero, (x, y) =>
        {
            float body = Mathf.Clamp01((0.35f - Mathf.Abs(y)) * 12f);
            float head = x > 0.4f ? Mathf.Clamp01(((1f - x) * 1.6f - Mathf.Abs(y)) * 12f) : body;
            return Mathf.Max(body * (x <= 0.4f ? 1f : 0f), head);
        }));

        static Sprite Build(string name, int size, Vector4 border, Func<float, float, float> alpha)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                name = name,
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
                hideFlags = HideFlags.DontSave,
            };

            var pixels = new Color32[size * size];
            for (int py = 0; py < size; py++)
            {
                for (int px = 0; px < size; px++)
                {
                    float x = (px + 0.5f) / size * 2f - 1f;
                    float y = (py + 0.5f) / size * 2f - 1f;
                    byte a = (byte)Mathf.RoundToInt(Mathf.Clamp01(alpha(x, y)) * 255f);
                    pixels[py * size + px] = new Color32(255, 255, 255, a);
                }
            }
            tex.SetPixels32(pixels);
            tex.Apply(false, true);

            var sprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, border);
            sprite.name = name;
            sprite.hideFlags = HideFlags.DontSave;
            return sprite;
        }

        /// <summary>Vertical two-colour gradient texture for backgrounds.</summary>
        public static Texture2D Gradient(Color top, Color bottom, int height = 128)
        {
            var tex = new Texture2D(2, height, TextureFormat.RGBA32, false)
            {
                name = "dp_gradient",
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
                hideFlags = HideFlags.DontSave,
            };
            var pixels = new Color[2 * height];
            for (int y = 0; y < height; y++)
            {
                float t = y / (float)(height - 1);
                var c = Color.Lerp(bottom, top, t * t * (3f - 2f * t));
                pixels[y * 2] = c;
                pixels[y * 2 + 1] = c;
            }
            tex.SetPixels(pixels);
            tex.Apply(false, false);
            return tex;
        }
    }
}
