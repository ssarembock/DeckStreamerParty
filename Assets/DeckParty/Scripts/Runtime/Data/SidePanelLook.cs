using System;
using UnityEngine;

namespace DeckParty
{
    /// <summary>
    /// UI Skin > Players and emotes (2026-11-18): the colours inside the left column's cards in multiplayer games, the player
    /// rows and the emote buttons. The cards themselves (inside, border, header text) are Site Colours > Players / Emotes, and
    /// the rows' texts are Text Colours > Player rows. Applied live while playing. The lobby keeps its own (dark menu) rows.
    /// </summary>
    [Serializable]
    public sealed class SidePanelLook
    {
        // ------------------------------------------------------------------ player rows

        [Tooltip("The background of each player's row.")]
        public Color rowColour = UIPalette.PanelAlt;
        [Tooltip("Your own row leans this far towards your player colour (0 = the same as the others).")]
        [Range(0f, 1f)] public float ownRowTint = 0.15f;
        [Tooltip("Tick to give your own row a fixed colour instead of the lean towards your player colour.")]
        public OptionalColour ownRowColour = new OptionalColour(UIPalette.PanelAlt);
        [Tooltip("The row of the stream on screen leans this far towards that player's colour.")]
        [Range(0f, 1f)] public float watchedRowTint = 0.3f;
        [Tooltip("Tick to give the row of the stream on screen a fixed colour instead.")]
        public OptionalColour watchedRowColour = new OptionalColour(UIPalette.PanelAlt);
        [Tooltip("Outline the row of the stream on screen.")]
        public bool watchedOutline = true;
        [Tooltip("Tick to give that outline a fixed colour (unticked: the player's colour).")]
        public OptionalColour watchedOutlineColour = new OptionalColour(Color.white);
        [Tooltip("Multiplies the row's colour while the pointer is over it (white = no change).")]
        public Color rowHoverTint = Color.Lerp(Color.white, UIPalette.Teal, 0.35f);
        [Tooltip("Multiplies the row's colour while it's pressed (white = no change).")]
        public Color rowPressedTint = Color.Lerp(Color.white, UIPalette.Teal, 0.6f);

        // ------------------------------------------------------------------ emote buttons

        [Tooltip("The background of each emote button.")]
        public Color emoteColour = UIPalette.PanelAlt;
        [Tooltip("Multiplies the button's colour while the pointer is over it (white = no change).")]
        public Color emoteHoverTint = Color.Lerp(Color.white, UIPalette.Pink, 0.35f);
        [Tooltip("Multiplies the button's colour while it's pressed (white = no change).")]
        public Color emotePressedTint = Color.Lerp(Color.white, UIPalette.Pink, 0.6f);
        [Tooltip("How see-through a locked emote's button is (its picture dims too).")]
        [Range(0f, 1f)] public float lockedEmoteAlpha = 0.35f;
        [Tooltip("Multiplies the emote pictures (white = their own colours).")]
        public Color emoteArtTint = Color.white;
        [Tooltip("The colour of text emotes (shown until an emote has a picture).")]
        public Color emoteTextColour = UIPalette.Text;

        /// <summary>The UI Skin's look, or null without a Game Database.</summary>
        public static SidePanelLook Current
        {
            get
            {
                var skin = GameDatabase.Instance != null ? GameDatabase.Instance.skin : null;
                return skin != null ? skin.sidePanel : null;
            }
        }

        /// <summary>The look to use: the UI Skin's, or the built-in one.</summary>
        public static SidePanelLook OrDefault => Current ?? s_Default;

        static readonly SidePanelLook s_Default = new SidePanelLook();

        /// <summary>A player row's background: the row colour, your own row's, or the on-screen stream's.</summary>
        public Color RowColour(Color playerColour, bool own, bool watched)
        {
            if (watched) return watchedRowColour.use ? watchedRowColour.colour : Color.Lerp(rowColour, playerColour, watchedRowTint);
            if (own) return ownRowColour.use ? ownRowColour.colour : Color.Lerp(rowColour, playerColour, ownRowTint);
            return rowColour;
        }
    }
}
