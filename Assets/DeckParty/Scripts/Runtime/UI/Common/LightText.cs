using System.Text.RegularExpressions;
using UnityEngine;

namespace DeckParty.UI
{
    /// <summary>
    /// Keeps text readable on the white pop-ups (tooltips, dialogs, banners, summaries). Most rich text in the game was
    /// written for dark panels: bright gold, teal and green numbers, white names, lilac hints. On white those wash out, so
    /// any colour too light to read is darkened to the same hue (greys and whites become the site's black or dark grey).
    /// Dark and saturated colours (red, pink, purple, black) are left alone.
    /// </summary>
    public static class LightText
    {
        /// <summary>Above this luminance a colour is too pale for text on white.</summary>
        const float MaxLuminance = 0.42f;

        static readonly Regex s_ColorTag = new Regex("<color=#([0-9A-Fa-f]{6})([0-9A-Fa-f]{2})?>", RegexOptions.Compiled);

        /// <summary>Rewrites every <c>&lt;color=#RRGGBB&gt;</c> tag that would be too pale on white. Idempotent.</summary>
        public static string Adapt(string text)
        {
            if (string.IsNullOrEmpty(text) || text.IndexOf("<color=#", System.StringComparison.OrdinalIgnoreCase) < 0) return text;
            return s_ColorTag.Replace(text, match =>
            {
                if (!ColorUtility.TryParseHtmlString("#" + match.Groups[1].Value, out var color)) return match.Value;
                var readable = Readable(color);
                if (readable == color) return match.Value;
                return "<color=#" + ColorUtility.ToHtmlStringRGB(readable) + match.Groups[2].Value + ">";
            });
        }

        /// <summary>A version of <paramref name="color"/> that reads on white (the colour itself when it already does).</summary>
        public static Color Readable(Color color)
        {
            float luminance = Luminance(color);
            if (luminance <= MaxLuminance) return color;
            Color.RGBToHSV(color, out _, out float saturation, out _);
            // Whites and pale greys: the site's text colours.
            if (saturation < 0.2f) return luminance > 0.8f ? SitePalette.Text.WithAlpha(color.a) : SitePalette.TextDim.WithAlpha(color.a);
            // Same hue, darker: scale towards black until it's as dark as the limit (a touch under, so it's clearly legible).
            float scale = (MaxLuminance - 0.06f) / luminance;
            return new Color(color.r * scale, color.g * scale, color.b * scale, color.a);
        }

        public static float Luminance(Color c) => 0.2126f * c.r + 0.7152f * c.g + 0.0722f * c.b;
    }
}
