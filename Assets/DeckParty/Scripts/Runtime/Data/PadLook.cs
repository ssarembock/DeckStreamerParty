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

    /// <summary>How the gamepad selector frame eases when it moves to the next thing.</summary>
    public enum PadFocusEase
    {
        Linear = 0,
        Smooth = 1,
        EaseInOut = 2,
        Overshoot = 3,
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
        [Tooltip("Seconds the frame takes to move to the next thing (0 = jump straight there). Bigger is slower.")]
        [Range(0f, 1f)] public float focusMoveTime = 0.2f;
        [Tooltip("How the move eases: Linear (constant speed), Smooth (slows into place), Ease In Out (speeds up, then slows) or Overshoot (springs a little past and settles).")]
        public PadFocusEase focusMoveEase = PadFocusEase.Smooth;

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

        /// <summary><paramref name="t"/> (0 to 1) eased the way <see cref="focusMoveEase"/> says.</summary>
        public float Ease(float t)
        {
            t = Mathf.Clamp01(t);
            switch (focusMoveEase)
            {
                case PadFocusEase.Linear:
                    return t;
                case PadFocusEase.EaseInOut:
                    return t < 0.5f ? 4f * t * t * t : 1f - Mathf.Pow(-2f * t + 2f, 3f) * 0.5f;
                case PadFocusEase.Overshoot:
                {
                    const float c1 = 1.70158f;
                    const float c3 = c1 + 1f;
                    float u = t - 1f;
                    return 1f + c3 * u * u * u + c1 * u * u;
                }
                default:
                {
                    float u = 1f - t;
                    return 1f - u * u * u;
                }
            }
        }
    }
}
