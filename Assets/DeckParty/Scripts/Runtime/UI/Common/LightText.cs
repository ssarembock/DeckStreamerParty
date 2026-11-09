using System.Text.RegularExpressions;
using UnityEngine;

namespace DeckParty.UI
{
    /// <summary>
    /// Keeps text readable on the white pop-ups (tooltips, dialogs, banners, summaries). Most rich text in the game was
    /// written for dark panels: bright gold, teal and green numbers, white names, lilac hints. On white those wash out, so
    /// any colour too light to read is darkened to the same hue (greys and whites become the site's black or dark grey).
    /// Dark and saturated colours (red, pink, purple, black) are left alone. The "On" versions follow the UI Skin's Site
    /// Colours: on a panel given a dark inside colour nothing is changed, and greys become that panel's text colours.
    /// </summary>
    public static class LightText
    {
        /// <summary>Above this luminance a colour is too pale for text on white.</summary>
        const float MaxLuminance = 0.42f;

        static readonly Regex s_ColorTag = new Regex("<color=#([0-9A-Fa-f]{6})([0-9A-Fa-f]{2})?>", RegexOptions.Compiled);

        /// <summary>Rewrites every <c>&lt;color=#RRGGBB&gt;</c> tag that would be too pale on white. Idempotent.</summary>
        public static string Adapt(string text) => Adapt(text, SitePalette.Text, SitePalette.TextDim);

        /// <summary>
        /// <see cref="Adapt(string)"/> for a text on the website look: nothing changes when its panel's inside is dark, and
        /// greys become that panel's text colours.
        /// </summary>
        public static string AdaptOn(Transform on, string text) =>
            TryLightPanel(on, out var ink, out var dim) ? Adapt(text, ink, dim) : text;

        static string Adapt(string text, Color ink, Color dim)
        {
            if (string.IsNullOrEmpty(text) || text.IndexOf("<color=#", System.StringComparison.OrdinalIgnoreCase) < 0) return text;
            return s_ColorTag.Replace(text, match =>
            {
                if (!ColorUtility.TryParseHtmlString("#" + match.Groups[1].Value, out var color)) return match.Value;
                var readable = Readable(color, ink, dim);
                if (readable == color) return match.Value;
                return "<color=#" + ColorUtility.ToHtmlStringRGB(readable) + match.Groups[2].Value + ">";
            });
        }

        /// <summary>A version of <paramref name="color"/> that reads on white (the colour itself when it already does).</summary>
        public static Color Readable(Color color) => Readable(color, SitePalette.Text, SitePalette.TextDim);

        /// <summary><see cref="Readable(Color)"/> for a text on the website look (see <see cref="AdaptOn"/>).</summary>
        public static Color ReadableOn(Transform on, Color color) =>
            TryLightPanel(on, out var ink, out var dim) ? Readable(color, ink, dim) : color;

        static Color Readable(Color color, Color ink, Color dim)
        {
            float luminance = Luminance(color);
            if (luminance <= MaxLuminance) return color;
            Color.RGBToHSV(color, out _, out float saturation, out _);
            // Whites and pale greys: the panel's text colours.
            if (saturation < 0.2f) return luminance > 0.8f ? ink.WithAlpha(color.a) : dim.WithAlpha(color.a);
            // Same hue, darker: scale towards black until it's as dark as the limit (a touch under, so it's clearly legible).
            float scale = (MaxLuminance - 0.06f) / luminance;
            return new Color(color.r * scale, color.g * scale, color.b * scale, color.a);
        }

        /// <summary>
        /// Whether the text sits on a light panel (pale colours need darkening), with that panel's text colours. Text outside
        /// the website look counts as on white, as before.
        /// </summary>
        static bool TryLightPanel(Transform on, out Color ink, out Color dim)
        {
            if (!SiteColours.IsSite(on))
            {
                ink = SitePalette.Text;
                dim = SitePalette.TextDim;
                return true;
            }
            var area = SiteColours.AreaOf(on);
            ink = SiteColours.Get(SiteColourRole.Text, area);
            dim = SiteColours.Get(SiteColourRole.SecondaryText, area);
            return SiteColours.IsLight(SiteColours.Get(SiteColourRole.PanelFill, area));
        }

        public static float Luminance(Color c) => 0.2126f * c.r + 0.7152f * c.g + 0.0722f * c.b;
    }
}
