using UnityEngine;

namespace DeckParty
{
    /// <summary>
    /// The light "streaming website" look of the stream and the shop: white surfaces on a pale page, near-black borders on
    /// the chat, grey dividers and dark text. Menus, the select screen and pop-up dialogs keep the dark <see cref="UIPalette"/>.
    /// The one colour that changes per streamer is the platform accent (<see cref="SiteTheme"/>).
    /// </summary>
    public static class SitePalette
    {
        /// <summary>The page behind everything.</summary>
        public static readonly Color Page = UIPalette.Hex(0xF4F4F6);
        /// <summary>Cards, panels, the header bar and the chat.</summary>
        public static readonly Color Surface = UIPalette.Hex(0xFFFFFF);
        /// <summary>The chat's typing area, secondary buttons, empty seats and inactive chips.</summary>
        public static readonly Color SurfaceAlt = UIPalette.Hex(0xEFEFF1);
        /// <summary>Panel borders.</summary>
        public static readonly Color Border = UIPalette.Hex(0xD6D6DC);
        /// <summary>The chat window's border (the brief: black bordered, slightly rounded).</summary>
        public static readonly Color Outline = UIPalette.Hex(0x18181B);
        /// <summary>Hairlines between sections.</summary>
        public static readonly Color Divider = UIPalette.Hex(0xE3E3E8);
        public static readonly Color Text = UIPalette.Hex(0x0E0E10);
        public static readonly Color TextDim = UIPalette.Hex(0x53535F);
        public static readonly Color TextFaint = UIPalette.Hex(0x9A9AA6);
        /// <summary>Dark pills that hold coloured numbers (stats, projections) so gold and green stay readable on white.</summary>
        public static readonly Color Pill = UIPalette.Hex(0x18181B, 0.92f);
        /// <summary>Unlit pips and other "off" icons.</summary>
        public static readonly Color Off = UIPalette.Hex(0xD3D3D9);
        public static readonly Color Danger = UIPalette.Hex(0xE5484D);
        public static readonly Color Warning = UIPalette.Hex(0xF08C00);
        public static readonly Color Gold = UIPalette.Hex(0xF2B300);
        /// <summary>The default accent, used until a platform is known.</summary>
        public static readonly Color Accent = UIPalette.Hex(0x8A4FFF);
    }
}
