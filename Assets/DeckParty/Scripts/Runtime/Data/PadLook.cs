using System;
using UnityEngine;

namespace DeckParty
{
    /// <summary>Which corner of a button its gamepad prompt sits on.</summary>
    public enum PadPromptCorner
    {
        TopRight = 0,
        TopLeft = 1,
        BottomRight = 2,
        BottomLeft = 3,
    }

    /// <summary>
    /// UI Skin > Controller: how the gamepad selector and the button prompts look. The art itself (the selector frame and
    /// one glyph per pad button) is in the UI Skin's Controller entries; their Override Colour tints them.
    /// </summary>
    [Serializable]
    public sealed class PadLook
    {
        [Tooltip("Space between the highlighted thing and the selector frame, in pixels (negative draws it inside).")]
        public float focusPadding = 10f;
        [Tooltip("How much the selector frame breathes (0 = still).")]
        [Range(0f, 0.2f)] public float focusPulse = 0.04f;
        [Tooltip("Breaths per second.")]
        [Range(0.2f, 4f)] public float focusPulseSpeed = 1.6f;
        [Tooltip("The frame's colour when the Controller selector frame entry has no Override Colour.")]
        public Color focusColour = new Color(0.54f, 0.31f, 1f, 1f);
        [Tooltip("Seconds the frame takes to glide to the next thing (0 = jump).")]
        [Range(0f, 0.3f)] public float focusGlide = 0.08f;

        [Tooltip("Show the bound pad button on the stream and shop buttons while a controller is in use.")]
        public bool showPrompts = true;
        [Tooltip("Size of a button prompt, in pixels.")]
        public float promptSize = 44f;
        [Tooltip("Which corner of the button the prompt sits on.")]
        public PadPromptCorner promptCorner = PadPromptCorner.TopRight;
        [Tooltip("How far the prompt hangs over the button's edge, in pixels.")]
        public float promptOverhang = 12f;

        [Tooltip("Hide the mouse cursor while the controller is in use (it comes back when the mouse moves).")]
        public bool hideCursor = true;

        /// <summary>The UI Skin's look, or null without a Game Database.</summary>
        public static PadLook Current
        {
            get
            {
                var skin = GameDatabase.Instance != null ? GameDatabase.Instance.skin : null;
                return skin != null ? skin.pad : null;
            }
        }

        /// <summary>The look to use: the UI Skin's, or the built-in one.</summary>
        public static PadLook OrDefault => Current ?? s_Default;

        static readonly PadLook s_Default = new PadLook();
    }
}
