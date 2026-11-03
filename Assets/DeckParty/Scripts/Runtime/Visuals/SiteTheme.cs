using System;
using UnityEngine;

namespace DeckParty
{
    /// <summary>
    /// Which satirical platform the stream and shop on screen are styled as: the board's streamer picks it (Game Database >
    /// Streamers > Platform). The animated logo (<see cref="UI.PlatformLogoView"/>) and every <see cref="UI.SiteAccent"/>
    /// follow it. Only colours and the logo change; layout is the same on every platform.
    /// </summary>
    public static class SiteTheme
    {
        static bool s_Set;

        public static StreamPlatform Platform { get; private set; } = StreamPlatform.Lookout;
        /// <summary>The platform's button colour.</summary>
        public static Color Accent { get; private set; } = SitePalette.Accent;
        /// <summary>The icon colour that reads on <see cref="Accent"/>.</summary>
        public static Color AccentContent { get; private set; } = Color.white;

        public static event Action Changed;

        /// <summary>Switches the look (no-op when it's already this platform, unless <paramref name="force"/>).</summary>
        public static void Apply(StreamPlatform platform, bool force = false)
        {
            if (s_Set && platform == Platform && !force) return;
            s_Set = true;
            Platform = platform;
            var art = GameDatabase.Instance.GetPlatformArt(platform);
            Accent = art != null ? art.accent.WithAlpha(1f) : PlatformArt.DefaultAccent(platform);
            AccentContent = art != null ? art.accentContent.WithAlpha(1f) : PlatformArt.DefaultContent(platform);
            try { Changed?.Invoke(); }
            catch (Exception e) { Debug.LogException(e); }
        }

        /// <summary>The art entry for the current platform (null until the Game Database has one; built-in shapes are used then).</summary>
        public static PlatformArt Art => GameDatabase.Instance.GetPlatformArt(Platform);

        /// <summary>A softer version of the accent for hover states and tints.</summary>
        public static Color AccentSoft => Color.Lerp(Accent, Color.white, 0.82f);

        /// <summary>
        /// The accent made readable as text or a thin line on white (very light accents, like neon green, are darkened).
        /// </summary>
        public static Color AccentOnWhite
        {
            get
            {
                float luminance = 0.2126f * Accent.r + 0.7152f * Accent.g + 0.0722f * Accent.b;
                return luminance > 0.6f ? Color.Lerp(Accent, Color.black, 0.45f) : Accent;
            }
        }
    }
}
