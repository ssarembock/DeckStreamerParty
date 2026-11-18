using System;
using UnityEngine;

namespace DeckParty.UI
{
    /// <summary>
    /// What a gamepad button can do. The selector (D-pad / left stick) is not here: it always moves with the D-pad and the
    /// left stick. Values are saved in the player's bindings, so only append.
    /// </summary>
    public enum PadAction : byte
    {
        /// <summary>Press the highlighted thing (buy a card, use a viewer's action, pick a target, press a button).</summary>
        Confirm = 0,
        /// <summary>Back out: close a pop-up, cancel picking a target, go back a screen.</summary>
        Back = 1,
        /// <summary>The game menu (pause).</summary>
        Menu = 2,
        /// <summary>The Audience overview.</summary>
        Audience = 3,
        // --- the stream ---
        AddViewer = 4,
        EndStream = 5,
        // --- the shop ---
        GoLive = 6,
        RaiseSubCap = 7,
        UseActive = 8,
        RemoveViewer = 9,
        AddStream = 10,
        AddShopViewer = 11,
        /// <summary>Held: shows the pad button prompts (when Settings > Controller > Show controller shortcuts is Hold).</summary>
        ShowShortcuts = 12,
    }

    /// <summary>When an action works: everywhere, during a stream or in the shop. Actions of different phases may share a button.</summary>
    public enum PadContext
    {
        Everywhere,
        Stream,
        Shop,
    }

    /// <summary>
    /// The gamepad buttons an action can be bound to (Xbox layout names; on other pads the same positions). The D-pad and
    /// the left stick move the selector and can't be bound. Values are saved, so only append.
    /// </summary>
    public enum PadButton : byte
    {
        None = 0,
        /// <summary>A (Cross on PlayStation).</summary>
        South = 1,
        /// <summary>B (Circle).</summary>
        East = 2,
        /// <summary>X (Square).</summary>
        West = 3,
        /// <summary>Y (Triangle).</summary>
        North = 4,
        LeftShoulder = 5,
        RightShoulder = 6,
        LeftTrigger = 7,
        RightTrigger = 8,
        /// <summary>View / Back / Share.</summary>
        Select = 9,
        /// <summary>Menu / Start / Options.</summary>
        Start = 10,
        LeftStickPress = 11,
        RightStickPress = 12,
    }

    /// <summary>
    /// The player's gamepad mapping: one button per action, saved on this PC (Settings > Controller changes it). Actions
    /// that never happen at the same time (Add Viewer in a stream, Raise Sub Cap in the shop) may share a button.
    /// </summary>
    public static class PadBindings
    {
        const string PrefsKey = "dp.pad.bindings.v2";
        /// <summary>The first mapping (2026-11-17, before Show Shortcuts took LB): read once and moved to <see cref="PrefsKey"/>.</summary>
        const string PrefsKeyV1 = "dp.pad.bindings.v1";

        public static readonly PadAction[] All =
        {
            PadAction.Confirm, PadAction.Back, PadAction.Menu, PadAction.Audience, PadAction.ShowShortcuts,
            PadAction.AddViewer, PadAction.EndStream,
            PadAction.GoLive, PadAction.RaiseSubCap, PadAction.UseActive, PadAction.RemoveViewer, PadAction.AddStream, PadAction.AddShopViewer,
        };

        /// <summary>Every bindable button, in the order a pad shows them.</summary>
        public static readonly PadButton[] Buttons =
        {
            PadButton.South, PadButton.East, PadButton.West, PadButton.North, PadButton.LeftShoulder, PadButton.RightShoulder,
            PadButton.LeftTrigger, PadButton.RightTrigger, PadButton.Select, PadButton.Start, PadButton.LeftStickPress, PadButton.RightStickPress,
        };

        static PadButton[] s_Bindings;

        /// <summary>The mapping changed (prompts redraw).</summary>
        public static event Action Changed;

        public static PadButton Default(PadAction action)
        {
            switch (action)
            {
                case PadAction.Confirm: return PadButton.South;
                case PadAction.Back: return PadButton.East;
                case PadAction.Menu: return PadButton.Start;
                case PadAction.Audience: return PadButton.Select;
                case PadAction.AddViewer: return PadButton.West;
                case PadAction.EndStream: return PadButton.North;
                case PadAction.GoLive: return PadButton.North;
                case PadAction.RaiseSubCap: return PadButton.West;
                case PadAction.UseActive: return PadButton.RightShoulder;
                case PadAction.RemoveViewer: return PadButton.LeftTrigger;
                case PadAction.AddStream: return PadButton.RightTrigger;
                case PadAction.AddShopViewer: return PadButton.RightStickPress;
                case PadAction.ShowShortcuts: return PadButton.LeftShoulder;
                default: return PadButton.None;
            }
        }

        /// <summary>The defaults of the first mapping (v1), where they differ: a v1 binding still on its old default takes the new one.</summary>
        static PadButton DefaultV1(PadAction action)
        {
            switch (action)
            {
                case PadAction.RemoveViewer: return PadButton.LeftShoulder;
                case PadAction.AddStream: return PadButton.LeftTrigger;
                case PadAction.AddShopViewer: return PadButton.RightTrigger;
                default: return Default(action);
            }
        }

        public static PadContext ContextOf(PadAction action)
        {
            switch (action)
            {
                case PadAction.AddViewer:
                case PadAction.EndStream:
                    return PadContext.Stream;
                case PadAction.GoLive:
                case PadAction.RaiseSubCap:
                case PadAction.UseActive:
                case PadAction.RemoveViewer:
                case PadAction.AddStream:
                case PadAction.AddShopViewer:
                    return PadContext.Shop;
                default:
                    return PadContext.Everywhere;
            }
        }

        /// <summary>Whether two actions can be pressed in the same moment (then they can't share a button).</summary>
        public static bool Overlap(PadAction a, PadAction b)
        {
            var ca = ContextOf(a);
            var cb = ContextOf(b);
            return ca == PadContext.Everywhere || cb == PadContext.Everywhere || ca == cb;
        }

        public static PadButton Get(PadAction action)
        {
            Load();
            int index = (int)action;
            return index >= 0 && index < s_Bindings.Length ? s_Bindings[index] : PadButton.None;
        }

        /// <summary>
        /// Binds <paramref name="button"/> to <paramref name="action"/>. An action that could be pressed at the same time and
        /// already used that button gets this action's old button instead (a swap), so nothing is ever left unreachable.
        /// </summary>
        public static void Set(PadAction action, PadButton button)
        {
            Load();
            var old = Get(action);
            if (old == button) return;
            foreach (var other in All)
                if (other != action && Get(other) == button && Overlap(action, other)) s_Bindings[(int)other] = old;
            s_Bindings[(int)action] = button;
            Save();
        }

        public static void ResetAll()
        {
            Load();
            foreach (var action in All) s_Bindings[(int)action] = Default(action);
            Save();
        }

        /// <summary>Confirm and Back must always work: they can't be left unbound.</summary>
        public static bool Required(PadAction action) => action == PadAction.Confirm || action == PadAction.Back;

        static void Load()
        {
            if (s_Bindings != null) return;
            int size = 0;
            foreach (var action in All) size = Mathf.Max(size, (int)action + 1);
            s_Bindings = new PadButton[size];
            foreach (var action in All) s_Bindings[(int)action] = Default(action);

            bool fromV1 = false;
            string saved = PlayerPrefs.GetString(PrefsKey, string.Empty);
            if (string.IsNullOrEmpty(saved))
            {
                saved = PlayerPrefs.GetString(PrefsKeyV1, string.Empty);
                fromV1 = !string.IsNullOrEmpty(saved);
            }
            if (string.IsNullOrEmpty(saved)) return;

            // "action:button,action:button..." so actions added later keep their defaults.
            var loaded = new bool[size];
            foreach (var pair in saved.Split(','))
            {
                var parts = pair.Split(':');
                if (parts.Length != 2 || !int.TryParse(parts[0], out int a) || !int.TryParse(parts[1], out int b)) continue;
                if (a < 0 || a >= s_Bindings.Length || b < 0 || b > byte.MaxValue || !Enum.IsDefined(typeof(PadButton), (byte)b)) continue;
                // A v1 binding still on the old default moves to the new default.
                if (fromV1 && (PadButton)b == DefaultV1((PadAction)a)) continue;
                s_Bindings[a] = (PadButton)b;
                loaded[a] = true;
            }

            // An action added since the save keeps its default; a saved action on the same button moves to a free one.
            foreach (var added in All)
            {
                if (loaded[(int)added] || s_Bindings[(int)added] == PadButton.None) continue;
                foreach (var other in All)
                    if (other != added && loaded[(int)other] && s_Bindings[(int)other] == s_Bindings[(int)added] && Overlap(added, other))
                        s_Bindings[(int)other] = FreeButtonFor(other);
            }

            if (fromV1)
            {
                Save();
                PlayerPrefs.DeleteKey(PrefsKeyV1);
            }
        }

        /// <summary>The action's default button if nothing that overlaps it uses it, else the first such free button, else None.</summary>
        static PadButton FreeButtonFor(PadAction action)
        {
            bool Taken(PadButton button)
            {
                foreach (var other in All)
                    if (other != action && s_Bindings[(int)other] == button && Overlap(action, other)) return true;
                return false;
            }
            if (!Taken(Default(action))) return Default(action);
            foreach (var button in Buttons)
                if (!Taken(button)) return button;
            return PadButton.None;
        }

        static void Save()
        {
            var parts = new System.Text.StringBuilder();
            foreach (var action in All)
            {
                if (parts.Length > 0) parts.Append(',');
                parts.Append((int)action).Append(':').Append((int)s_Bindings[(int)action]);
            }
            PlayerPrefs.SetString(PrefsKey, parts.ToString());
            PlayerPrefs.Save();
            Changed?.Invoke();
        }

        // ------------------------------------------------------------------ names and glyphs

        /// <summary>The CSV key of an action's name ("pad.action.AddViewer").</summary>
        public static string ActionKey(PadAction action) => "pad.action." + action;

        /// <summary>A button's short label for prompts drawn without art: "A", "LB", "RT", "View"...</summary>
        public static string ShortLabel(PadButton button)
        {
            switch (button)
            {
                case PadButton.South: return "A";
                case PadButton.East: return "B";
                case PadButton.West: return "X";
                case PadButton.North: return "Y";
                case PadButton.LeftShoulder: return "LB";
                case PadButton.RightShoulder: return "RB";
                case PadButton.LeftTrigger: return "LT";
                case PadButton.RightTrigger: return "RT";
                case PadButton.Select: return "View";
                case PadButton.Start: return "Menu";
                case PadButton.LeftStickPress: return "LS";
                case PadButton.RightStickPress: return "RS";
                default: return "-";
            }
        }

        /// <summary>The UI Skin entry with this button's glyph (your controller art).</summary>
        public static SkinSlot GlyphSlot(PadButton button)
        {
            switch (button)
            {
                case PadButton.South: return SkinSlot.PadSouth;
                case PadButton.East: return SkinSlot.PadEast;
                case PadButton.West: return SkinSlot.PadWest;
                case PadButton.North: return SkinSlot.PadNorth;
                case PadButton.LeftShoulder: return SkinSlot.PadLeftShoulder;
                case PadButton.RightShoulder: return SkinSlot.PadRightShoulder;
                case PadButton.LeftTrigger: return SkinSlot.PadLeftTrigger;
                case PadButton.RightTrigger: return SkinSlot.PadRightTrigger;
                case PadButton.Select: return SkinSlot.PadSelect;
                case PadButton.Start: return SkinSlot.PadStart;
                case PadButton.LeftStickPress: return SkinSlot.PadLeftStick;
                default: return SkinSlot.PadRightStick;
            }
        }

        /// <summary>The placeholder colour of a face button (the usual A green, B red, X blue, Y yellow), grey for the rest.</summary>
        public static Color PlaceholderColour(PadButton button)
        {
            switch (button)
            {
                case PadButton.South: return UIPalette.Hex(0x3BAA5C);
                case PadButton.East: return UIPalette.Hex(0xD9453B);
                case PadButton.West: return UIPalette.Hex(0x2F7FE0);
                case PadButton.North: return UIPalette.Hex(0xE3A900);
                default: return UIPalette.Hex(0x3A3A44);
            }
        }
    }
}
