using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace DeckParty.UI
{
    public enum HotKey
    {
        Space,
        Enter,
        Escape,
        E,
        R,
        Tab,
        F9,
    }

    /// <summary>Keyboard / pointer access that works with either input backend.</summary>
    public static class InputHelper
    {
        public static Vector2 PointerPosition
        {
            get
            {
#if ENABLE_INPUT_SYSTEM
                var mouse = Mouse.current;
                if (mouse != null) return mouse.position.ReadValue();
                var touch = Touchscreen.current;
                if (touch != null) return touch.primaryTouch.position.ReadValue();
                return Vector2.zero;
#else
                return Input.mousePosition;
#endif
            }
        }

        public static bool Pressed(HotKey key)
        {
#if ENABLE_INPUT_SYSTEM
            var keyboard = Keyboard.current;
            if (keyboard == null) return false;
            switch (key)
            {
                case HotKey.Space: return keyboard.spaceKey.wasPressedThisFrame;
                case HotKey.Enter: return keyboard.enterKey.wasPressedThisFrame || keyboard.numpadEnterKey.wasPressedThisFrame;
                case HotKey.Escape: return keyboard.escapeKey.wasPressedThisFrame;
                case HotKey.E: return keyboard.eKey.wasPressedThisFrame;
                case HotKey.R: return keyboard.rKey.wasPressedThisFrame;
                case HotKey.Tab: return keyboard.tabKey.wasPressedThisFrame;
                case HotKey.F9: return keyboard.f9Key.wasPressedThisFrame;
            }
            return false;
#else
            switch (key)
            {
                case HotKey.Space: return Input.GetKeyDown(KeyCode.Space);
                case HotKey.Enter: return Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter);
                case HotKey.Escape: return Input.GetKeyDown(KeyCode.Escape);
                case HotKey.E: return Input.GetKeyDown(KeyCode.E);
                case HotKey.R: return Input.GetKeyDown(KeyCode.R);
                case HotKey.Tab: return Input.GetKeyDown(KeyCode.Tab);
                case HotKey.F9: return Input.GetKeyDown(KeyCode.F9);
            }
            return false;
#endif
        }

        /// <summary>True while a text field has keyboard focus (hotkeys should be ignored).</summary>
        public static bool IsTyping
        {
            get
            {
                var system = EventSystem.current;
                if (system == null) return false;
                var selected = system.currentSelectedGameObject;
                if (selected == null) return false;
                var field = selected.GetComponent<TMP_InputField>();
                return field != null && field.isFocused;
            }
        }
    }
}
