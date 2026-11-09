using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEditor;

namespace DeckParty.EditorTools
{
    /// <summary>
    /// What every UI Skin entry is: a readable name, the part of the game it belongs to and where it shows. The UI Skin
    /// inspector, the Skinned Image inspector and the Game Database's UI Icons list all read this table. When a builder
    /// starts using a slot somewhere new, add that place to the slot's description here.
    /// </summary>
    internal static class SkinSlotGuide
    {
        /// <summary>The part of the game an entry belongs to (the UI Skin inspector's sections, in this order).</summary>
        internal enum Area
        {
            Resources,
            MenuBar,
            Stream,
            Shop,
            Players,
            Cards,
            Menus,
            NotShown,
        }

        /// <summary>What kind of art an entry takes.</summary>
        internal enum Kind
        {
            /// <summary>A single picture (drawn as a Simple image).</summary>
            Icon,
            /// <summary>A panel, frame or button background that stretches (a 9-slice sprite).</summary>
            Frame,
            /// <summary>A plain shape: a circle, a ring or a soft glow.</summary>
            Shape,
        }

        internal sealed class Info
        {
            public readonly SkinSlot Slot;
            public readonly Area Area;
            public readonly string Name;
            public readonly Kind Kind;
            /// <summary>Where it shows in the game.</summary>
            public readonly string Where;
            /// <summary>The game sets this image's colour itself (by state or owner), so Override Colour has no lasting effect.</summary>
            public readonly bool GameColoured;

            public Info(SkinSlot slot, Area area, string name, Kind kind, string where, bool gameColoured = false)
            {
                Slot = slot;
                Area = area;
                Name = name;
                Kind = kind;
                Where = where;
                GameColoured = gameColoured;
            }
        }

        static readonly Dictionary<Area, (string title, string summary)> s_Areas = new Dictionary<Area, (string, string)>
        {
            { Area.Resources, ("Resources", "The resource icons: in the menu bar, on viewers and cards, and the small icons inside text.") },
            { Area.MenuBar, ("Menu bar", "The bar along the top of the stream and the shop.") },
            { Area.Stream, ("Stream", "The Stream Chat window (risk pips in its title band, the two main buttons along its bottom), the chat avatars and the waiting-viewer panel.") },
            { Area.Shop, ("Shop", "The shop's toolbar buttons.") },
            { Area.Players, ("Players", "Player rows in the lobby and in the Players panel (multiplayer).") },
            { Area.Cards, ("Viewer cards and placeholders", "The big viewer cards (Audience overview, viewer pickers, the Deplatformed screen) and the placeholder shown while a viewer has no art.") },
            { Area.Menus, ("Menus and dark pop-ups", "The dark menu look: main menu, character sets, multiplayer, lobby, settings, How to Play, streamer select, and in the game the pause menu and game over screen.") },
            { Area.NotShown, ("Not shown at the moment", "Nothing visible uses these right now. Changing them has no effect in the current UI.") },
        };

        static readonly Info[] s_All =
        {
            // --- Resources ---
            new Info(SkinSlot.IconPop, Area.Resources, "Clout icon", Kind.Icon,
                "The Clout counter in the menu bar, prices in the shop, and the small Clout icon inside text (card descriptions, tooltips, recaps and scores)."),
            new Info(SkinSlot.IconCash, Area.Resources, "Cash icon", Kind.Icon,
                "The Cash counter in the menu bar, and the small Cash icon inside text (card descriptions, tooltips, recaps and scores)."),
            new Info(SkinSlot.IconTrouble, Area.Resources, "Deplatform Risk icon", Kind.Icon,
                "The three risk pips in the title band at the top of the Stream Chat window, the risky-queue count under the Audience button, the risk badge on chat avatars and viewer cards, and the small Risk icon inside text."),
            new Info(SkinSlot.IconStar, Area.Resources, "Trophy icon", Kind.Icon,
                "The Trophies count in the middle of the menu bar (the icon and \"in chat / needed to win\"), the Trophy badge on chat avatars, viewer cards, viewer rows and shop tiles, the Trophies in the Players panel, and the small Trophy icon inside text."),
            new Info(SkinSlot.IconCancel, Area.Resources, "Cancels Risk badge", Kind.Icon,
                "The shield badge on chat avatars and viewer cards whose viewer cancels 1 Deplatform Risk (for example the Normie)."),

            // --- Menu bar ---
            new Info(SkinSlot.IconNight, Area.MenuBar, "Stream counter icon", Kind.Icon,
                "Beside the stream count (for example 3 / 25), next to the platform logo at the left of the menu bar."),
            new Info(SkinSlot.IconDoor, Area.Shop, "Sub Cap icon", Kind.Icon,
                "The Raise Sub Cap button in the shop toolbar. (The menu bar no longer shows the Sub Cap: the chat's seats show it.)"),
            new Info(SkinSlot.IconAudience, Area.MenuBar, "Audience button icon", Kind.Icon,
                "The Audience button in the menu bar and in the shop toolbar (it opens the Audience overview)."),
            new Info(SkinSlot.IconMenu, Area.MenuBar, "Menu button icon", Kind.Icon,
                "The Menu button at the right end of the menu bar (it opens the pause menu)."),
            new Info(SkinSlot.IconTimer, Area.MenuBar, "Countdown timer icon", Kind.Icon,
                "The hourglass beside the stream and shop countdowns. It only shows in matches that use timers."),

            // --- Stream ---
            new Info(SkinSlot.IconStreamChat, Area.NotShown, "Stream Chat title (not used)", Kind.Icon,
                "Nothing shows it at the moment: the Stream Chat window's title band now holds the Deplatform Risk pips (the Deplatform Risk icon entry)."),
            new Info(SkinSlot.IconCommunityBeats, Area.Stream, "Community Beats title", Kind.Icon,
                "The icon at the top of the Community Beats panel beside the chat (it replaces the words)."),
            new Info(SkinSlot.IconAddViewer, Area.Stream, "Add Viewer button icon", Kind.Icon,
                "The icon on the green Add Viewer button at the bottom of the Stream Chat window. The button is always green; only the icon comes from here."),
            new Info(SkinSlot.IconEndStream, Area.Stream, "End the Stream button icon", Kind.Icon,
                "The icon on the red End the Stream button at the bottom of the Stream Chat window. The button is always red; only the icon comes from here."),
            new Info(SkinSlot.AvatarRing, Area.Stream, "Avatar ring", Kind.Shape,
                "The round border of every chat avatar (with its highlight ring and the empty-seat ring), including the waiting viewer, and the round frames of the streamer portrait in the shop and in the stream recap. The game colours it by state.",
                gameColoured: true),
            new Info(SkinSlot.IconAction, Area.Stream, "Action button icon", Kind.Icon,
                "The small action button on chat avatars whose action is ready, and the Use button of the streamer's power in the shop."),
            new Info(SkinSlot.IconCheck, Area.Stream, "Tick icon (Approve, Confirm, Ready)", Kind.Icon,
                "Approve on the waiting-viewer panel, Confirm on the targeting bar, the ready tick on Go Live (Free For All), the ready tick in player rows, the tick on an armed streamer power, and the cleared tick on character sets."),
            new Info(SkinSlot.IconClose, Area.Stream, "Cross icon (Deny, Cancel, Sold out)", Kind.Icon,
                "Deny on the waiting-viewer panel, Cancel on the targeting bar, and the red cross over sold-out shop tiles."),

            // --- Shop ---
            new Info(SkinSlot.IconGoLive, Area.Shop, "Go Live button icon", Kind.Icon,
                "The play icon on the big Go Live button at the end of the shop toolbar."),
            new Info(SkinSlot.IconRemoveViewer, Area.Shop, "Remove a Viewer button icon", Kind.Icon,
                "The Remove a Viewer button in the shop toolbar."),

            // --- Players ---
            new Info(SkinSlot.IconCrown, Area.Players, "Host crown", Kind.Icon,
                "Beside the host's name in the lobby and in the Players panel."),
            new Info(SkinSlot.IconMute, Area.Players, "Mute emotes button icon", Kind.Icon,
                "The small round mute button on each player in the Players panel."),

            // --- Viewer cards and placeholders ---
            new Info(SkinSlot.CardFrame, Area.Cards, "Viewer card frame", Kind.Frame,
                "The frame of the big viewer cards (Audience overview, viewer pickers, the Deplatformed screen) and of the streamer tiles on the streamer select screen. The game colours it by the card's state.",
                gameColoured: true),
            new Info(SkinSlot.CardFrameStar, Area.Cards, "Trophy card frame", Kind.Frame,
                "Used instead of the viewer card frame on Trophy cards. Leave it empty to use the normal frame (tinted gold).",
                gameColoured: true),
            new Info(SkinSlot.SlotEmpty, Area.Cards, "Empty card outline", Kind.Frame,
                "The outline drawn where a big viewer card's spot is empty."),
            new Info(SkinSlot.Badge, Area.Cards, "Small badge", Kind.Frame,
                "The small badges on the big viewer cards (a permanent bonus, and the copies count in the Audience overview), and the countdown box on the streamer select screen."),
            new Info(SkinSlot.Circle, Area.Cards, "Circle (portrait placeholder)", Kind.Shape,
                "The coloured circle shown instead of a portrait while a viewer has no art (chat avatars, cards, rows and shop tiles), and the player colour dot in player rows. The game colours it with the viewer's or player's colour.",
                gameColoured: true),

            // --- Menus and dark pop-ups ---
            new Info(SkinSlot.Panel, Area.Menus, "Menu panel", Kind.Frame,
                "The dark panels of the menus: the main menu's button column, character sets, multiplayer, lobby (players, match settings, chat, join code), settings, How to Play and streamer select. In the game: the pause menu, the game over screen, the Deplatformed screen and the Activity log panel."),
            new Info(SkinSlot.PanelAlt, Area.Menus, "Inner panel and list row", Kind.Frame,
                "Rows and inner panels on top of a menu panel: character set list items, viewer rows (character set details, How to Play), player rows (lobby, Players panel, final standings) and the quick rules column of the pause menu."),
            new Info(SkinSlot.Button, Area.Menus, "Standard menu button", Kind.Frame,
                "Most menu buttons: Settings, How to Play, Back, Quit (main menu), Leave, Copy, the How to Play tabs, the < > arrows and ON/OFF switches in settings, the Activity and Chat tabs, and the second button on the game over screen."),
            new Info(SkinSlot.ButtonPrimary, Area.Menus, "Main choice button", Kind.Frame,
                "The main choice on each screen: Single Player, Host Online, Host LAN, Start, Continue, Confirm (streamer select), Resume, Send, the Rules tab, and the first button on the game over screen. The pink and the gold buttons both use this entry."),
            new Info(SkinSlot.ButtonSecondary, Area.Menus, "Secondary button", Kind.Frame,
                "Multiplayer, Join Online, Join LAN, Ready and Streamer in the menus, Apply in settings, and the action button on the big viewer cards."),
            new Info(SkinSlot.ButtonDanger, Area.Menus, "Danger button", Kind.Frame,
                "Quit in the pause menu, and Reset Progress in Settings."),
            new Info(SkinSlot.InputField, Area.Menus, "Text field and value box", Kind.Frame,
                "Text boxes (player name, join code, IP and port, chat, custom timer seconds) and the value box between the < > arrows of the settings selectors."),
            new Info(SkinSlot.ScrollHandle, Area.Menus, "Scrollbar handle", Kind.Frame,
                "The handle you drag on every scrollbar."),
            new Info(SkinSlot.Logo, Area.Menus, "Game logo (main menu)", Kind.Icon,
                "The game's logo at the top of the main menu. Leave it empty to show the title as text instead."),

            // --- Not shown at the moment ---
            new Info(SkinSlot.Glow, Area.NotShown, "Background glow", Kind.Shape,
                "The soft light spots of the stream setup background. The website page covers that background in the current look, so changes don't show."),
            new Info(SkinSlot.TopBar, Area.NotShown, "Top bar (not used)", Kind.Frame,
                "Nothing in the current UI uses it. The menu bar's colours are in Site Colours (Menu bar), at the top of the UI Skin."),
            new Info(SkinSlot.Tooltip, Area.NotShown, "Tooltip background (not used)", Kind.Frame,
                "Nothing in the current UI uses it. The tooltip box's colours are in Site Colours (Tooltips), at the top of the UI Skin."),
        };

        static Dictionary<SkinSlot, Info> s_Lookup;

        /// <summary>Every entry, in the order the UI Skin inspector shows them.</summary>
        public static IReadOnlyList<Info> All => s_All;

        public static string Title(Area area) => s_Areas[area].title;

        public static string Summary(Area area) => s_Areas[area].summary;

        public static Info Get(SkinSlot slot)
        {
            if (s_Lookup == null)
            {
                s_Lookup = new Dictionary<SkinSlot, Info>();
                foreach (var info in s_All) s_Lookup[info.Slot] = info;
            }
            // A slot added to the enum but not described here yet still gets a readable row.
            return s_Lookup.TryGetValue(slot, out var found)
                ? found
                : new Info(slot, Area.NotShown, ObjectNames.NicifyVariableName(slot.ToString()), Kind.Icon, "Not described yet (add it to SkinSlotGuide).");
        }

        /// <summary>
        /// The file name Deck Party > Art > Auto-Assign Sprites by Name puts into this entry (anywhere under Assets/DeckParty/Art):
        /// icons are "icon_...", everything else needs the "ui_" prefix.
        /// </summary>
        public static string FileName(SkinSlot slot)
        {
            string snake = Regex.Replace(slot.ToString(), "(?<=[a-z0-9])(?=[A-Z])", "_").ToLowerInvariant();
            return (snake.StartsWith("icon_") ? snake : "ui_" + snake) + ".png";
        }

        /// <summary>What art works for this kind of entry.</summary>
        public static string ArtAdvice(Kind kind)
        {
            switch (kind)
            {
                case Kind.Frame: return "Art: a 9-slice sprite. Set its borders in the Sprite Editor so the corners don't stretch.";
                case Kind.Shape: return "Art: a white shape on a transparent background (the game colours it).";
                default: return "Art: a square PNG with a transparent background, about 128 x 128 px.";
            }
        }
    }
}
