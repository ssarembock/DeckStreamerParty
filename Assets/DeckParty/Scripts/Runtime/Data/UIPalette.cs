using UnityEngine;

namespace DeckParty
{
    /// <summary>The default Deck Party colour scheme. Skin sprites override shapes; these remain the fallbacks.</summary>
    public static class UIPalette
    {
        public static readonly Color Night = Hex(0x160F29);
        public static readonly Color Panel = Hex(0x241A3D, 0.94f);
        public static readonly Color PanelAlt = Hex(0x2F2350);
        public static readonly Color PanelDeep = Hex(0x1C1433, 0.97f);
        public static readonly Color Border = Hex(0x4A3B78);
        public static readonly Color Text = Hex(0xF7F1FF);
        public static readonly Color TextDim = Hex(0xB9AED6);
        public static readonly Color Pink = Hex(0xFF4F8B);
        public static readonly Color Gold = Hex(0xFFC94A);
        public static readonly Color Teal = Hex(0x3DDBD9);
        public static readonly Color Green = Hex(0x6BE38B);
        public static readonly Color Red = Hex(0xFF5A5A);
        public static readonly Color Orange = Hex(0xFF9A3D);
        public static readonly Color Blue = Hex(0x5AA8FF);
        public static readonly Color Purple = Hex(0xA77BFF);
        public static readonly Color Shadow = new Color(0f, 0f, 0f, 0.55f);

        public static readonly Color[] PlayerColors =
        {
            Hex(0xFF4F8B), Hex(0x3DDBD9), Hex(0xFFC94A), Hex(0x6BE38B), Hex(0xA77BFF), Hex(0xFF9A3D),
        };

        public static Color PlayerColor(int index) => PlayerColors[Mathf.Abs(index) % PlayerColors.Length];

        public static string Tag(Color c) => "#" + ColorUtility.ToHtmlStringRGB(c);

        public static Color Hex(int rgb, float alpha = 1f)
        {
            return new Color(((rgb >> 16) & 0xFF) / 255f, ((rgb >> 8) & 0xFF) / 255f, (rgb & 0xFF) / 255f, alpha);
        }

        public static Color WithAlpha(this Color c, float a) => new Color(c.r, c.g, c.b, a);
    }
}
