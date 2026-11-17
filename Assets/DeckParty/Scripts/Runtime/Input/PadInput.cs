using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
#endif

namespace DeckParty.UI
{
    /// <summary>
    /// Gamepad input for the whole game. Any connected pad counts (couch play passes one pad, or each player uses their own
    /// on their turn). <see cref="PadRunner"/> updates it once a frame before anything reads it.
    /// <see cref="Active"/> is true from the moment a pad is used until the mouse or keyboard is: the selector, the button
    /// prompts and the hidden mouse cursor follow it.
    /// </summary>
    public static class PadInput
    {
        /// <summary>Seconds a direction is held before it repeats, then the repeat interval.</summary>
        const float RepeatDelay = 0.38f;
        const float RepeatRate = 0.11f;
        /// <summary>How far the left stick must lean to count as a direction.</summary>
        const float StickThreshold = 0.55f;

        static readonly bool[] s_Consumed = new bool[32];
        static Vector2Int s_HeldDirection;
        static float s_NextRepeat;
        static Vector2Int s_Navigate;
        static int s_Frame = -1;

        /// <summary>The pad is what the player is using right now (prompts, selector and hidden cursor).</summary>
        public static bool Active { get; private set; }

        /// <summary>A gamepad is plugged in.</summary>
        public static bool Connected
        {
            get
            {
#if ENABLE_INPUT_SYSTEM
                return Gamepad.all.Count > 0;
#else
                return false;
#endif
            }
        }

        /// <summary>Switched between pad and mouse/keyboard (true = pad).</summary>
        public static event System.Action<bool> ActiveChanged;

        /// <summary>Called by <see cref="PadRunner"/> at the start of every frame.</summary>
        internal static void Tick()
        {
            if (s_Frame == Time.frameCount) return;
            s_Frame = Time.frameCount;
            for (int i = 0; i < s_Consumed.Length; i++) s_Consumed[i] = false;
            s_Navigate = ReadNavigate();

            bool padUsed = AnyPadInput();
            bool otherUsed = !padUsed && MouseOrKeyboardUsed();
            if (padUsed && !Active) SetActive(true);
            else if (otherUsed && Active) SetActive(false);
        }

        static void SetActive(bool active)
        {
            Active = active;
            ActiveChanged?.Invoke(active);
        }

        // ------------------------------------------------------------------ buttons

        /// <summary>The action's button went down this frame on any pad (and nothing consumed it yet).</summary>
        public static bool Pressed(PadAction action)
        {
            if ((int)action < s_Consumed.Length && s_Consumed[(int)action]) return false;
            return Pressed(PadBindings.Get(action));
        }

        /// <summary>Marks the action handled for this frame, so a later reader doesn't act on the same press.</summary>
        public static void Consume(PadAction action)
        {
            if ((int)action < s_Consumed.Length) s_Consumed[(int)action] = true;
        }

        public static bool Pressed(PadButton button)
        {
#if ENABLE_INPUT_SYSTEM
            if (button == PadButton.None) return false;
            var pads = Gamepad.all;
            for (int i = 0; i < pads.Count; i++)
            {
                var control = Control(pads[i], button);
                if (control != null && control.wasPressedThisFrame) return true;
            }
#endif
            return false;
        }

        /// <summary>The first bindable button that went down this frame (Settings > Controller listens with it), or None.</summary>
        public static PadButton AnyButtonPressed()
        {
            foreach (var button in PadBindings.Buttons)
                if (Pressed(button)) return button;
            return PadButton.None;
        }

        // ------------------------------------------------------------------ the selector's direction

        /// <summary>
        /// The direction the selector should move this frame: (-1, 0) left, (1, 0) right, (0, 1) up, (0, -1) down, or zero.
        /// Holding a direction repeats it.
        /// </summary>
        public static Vector2Int Navigate => s_Navigate;

        static Vector2Int ReadNavigate()
        {
            var held = HeldDirection();
            if (held == Vector2Int.zero)
            {
                s_HeldDirection = Vector2Int.zero;
                return Vector2Int.zero;
            }
            float now = Time.unscaledTime;
            if (held != s_HeldDirection)
            {
                s_HeldDirection = held;
                s_NextRepeat = now + RepeatDelay;
                return held;
            }
            if (now < s_NextRepeat) return Vector2Int.zero;
            s_NextRepeat = now + RepeatRate;
            return held;
        }

        static Vector2Int HeldDirection()
        {
#if ENABLE_INPUT_SYSTEM
            var pads = Gamepad.all;
            for (int i = 0; i < pads.Count; i++)
            {
                var pad = pads[i];
                var dpad = pad.dpad.ReadValue();
                var stick = pad.leftStick.ReadValue();
                var value = dpad.sqrMagnitude > 0.1f ? dpad : stick;
                if (value.magnitude < StickThreshold) continue;
                // The stronger axis wins, so a diagonal lean picks one direction.
                return Mathf.Abs(value.x) >= Mathf.Abs(value.y)
                    ? new Vector2Int(value.x > 0f ? 1 : -1, 0)
                    : new Vector2Int(0, value.y > 0f ? 1 : -1);
            }
#endif
            return Vector2Int.zero;
        }

        /// <summary>The right stick, for scrolling long lists (y up), or zero.</summary>
        public static Vector2 Scroll
        {
            get
            {
#if ENABLE_INPUT_SYSTEM
                var pads = Gamepad.all;
                for (int i = 0; i < pads.Count; i++)
                {
                    var value = pads[i].rightStick.ReadValue();
                    if (value.magnitude > 0.2f) return value;
                }
#endif
                return Vector2.zero;
            }
        }

        // ------------------------------------------------------------------ which device is in use

        static bool AnyPadInput()
        {
#if ENABLE_INPUT_SYSTEM
            var pads = Gamepad.all;
            for (int i = 0; i < pads.Count; i++)
            {
                var pad = pads[i];
                if (pad.leftStick.ReadValue().magnitude > StickThreshold || pad.rightStick.ReadValue().magnitude > StickThreshold) return true;
                if (pad.dpad.ReadValue().sqrMagnitude > 0.1f) return true;
                foreach (var button in PadBindings.Buttons)
                {
                    var control = Control(pad, button);
                    if (control != null && control.wasPressedThisFrame) return true;
                }
            }
#endif
            return false;
        }

        static Vector2 s_LastMouse = new Vector2(float.NaN, float.NaN);

        static bool MouseOrKeyboardUsed()
        {
#if ENABLE_INPUT_SYSTEM
            var mouse = Mouse.current;
            if (mouse != null)
            {
                var position = mouse.position.ReadValue();
                bool moved = !float.IsNaN(s_LastMouse.x) && (position - s_LastMouse).sqrMagnitude > 16f;
                s_LastMouse = position;
                if (moved || mouse.leftButton.wasPressedThisFrame || mouse.rightButton.wasPressedThisFrame || mouse.scroll.ReadValue().sqrMagnitude > 0.01f) return true;
            }
            var keyboard = Keyboard.current;
            if (keyboard != null && keyboard.anyKey.wasPressedThisFrame) return true;
#endif
            return false;
        }

#if ENABLE_INPUT_SYSTEM
        static ButtonControl Control(Gamepad pad, PadButton button)
        {
            switch (button)
            {
                case PadButton.South: return pad.buttonSouth;
                case PadButton.East: return pad.buttonEast;
                case PadButton.West: return pad.buttonWest;
                case PadButton.North: return pad.buttonNorth;
                case PadButton.LeftShoulder: return pad.leftShoulder;
                case PadButton.RightShoulder: return pad.rightShoulder;
                case PadButton.LeftTrigger: return pad.leftTrigger;
                case PadButton.RightTrigger: return pad.rightTrigger;
                case PadButton.Select: return pad.selectButton;
                case PadButton.Start: return pad.startButton;
                case PadButton.LeftStickPress: return pad.leftStickButton;
                case PadButton.RightStickPress: return pad.rightStickButton;
                default: return null;
            }
        }
#endif
    }
}
