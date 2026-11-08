using System;
using UnityEngine;

namespace DeckParty
{
    /// <summary>
    /// Which satirical platform the stream and shop on screen belong to: the board's streamer picks it (Game Database >
    /// Streamers > Platform). Since 2026-11-08 the only thing that changes per platform is the animated logo in the menu bar
    /// (<see cref="UI.PlatformLogoView"/>). Colours and layout are the same on every platform: <see cref="Accent"/> is always
    /// <see cref="SitePalette.Accent"/>, Add Viewer is always green and End the Stream always red.
    /// </summary>
    public static class SiteTheme
    {
        static bool s_Set;

        public static StreamPlatform Platform { get; private set; } = StreamPlatform.Lookout;
        /// <summary>The site's accent (the same on every platform).</summary>
        public static Color Accent => SitePalette.Accent;
        /// <summary>The icon colour that reads on <see cref="Accent"/>.</summary>
        public static Color AccentContent => Color.white;

        /// <summary>Raised when the platform on screen changes (the logo swaps).</summary>
        public static event Action Changed;

        /// <summary>Switches the platform (no-op when it's already this platform, unless <paramref name="force"/>).</summary>
        public static void Apply(StreamPlatform platform, bool force = false)
        {
            if (s_Set && platform == Platform && !force) return;
            s_Set = true;
            Platform = platform;
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
