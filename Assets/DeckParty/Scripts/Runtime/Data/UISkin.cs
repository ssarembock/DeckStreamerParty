using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DeckParty
{
    /// <summary>
    /// Named UI art slots. Every skinned Image in the scenes and prefabs points at one of these. The readable names and where
    /// each one shows are in Editor/SkinSlotGuide.cs (the UI Skin inspector reads them): describe new slots there too.
    /// </summary>
    public enum SkinSlot
    {
        Panel = 0,
        PanelAlt = 1,
        Button = 2,
        ButtonPrimary = 3,
        ButtonDanger = 4,
        CardFrame = 5,
        CardFrameStar = 6,
        SlotEmpty = 7,
        Badge = 8,
        Circle = 9,
        IconPop = 10,
        IconCash = 11,
        IconTrouble = 12,
        IconStar = 13,
        IconCancel = 14,
        IconDoor = 15,
        IconCrown = 16,
        IconCheck = 17,
        Logo = 18,
        InputField = 19,
        TopBar = 20,
        Tooltip = 21,
        ScrollHandle = 22,
        Glow = 23,
        ButtonSecondary = 24,
        // Icon buttons of the stream and shop (V5: buttons show an icon, the words are in their hover tooltip).
        IconAddViewer = 25,
        IconEndStream = 26,
        IconAudience = 27,
        IconMenu = 28,
        IconAction = 29,
        IconGoLive = 30,
        IconClose = 31,
        IconNight = 32,
        /// <summary>The round border of the chat avatars (and their highlight).</summary>
        AvatarRing = 33,
        /// <summary>The icon in the Community Beats panel title.</summary>
        IconCommunityBeats = 34,
        /// <summary>The Stream Chat window's title (an icon instead of the words).</summary>
        IconStreamChat = 35,
        /// <summary>The countdown timers (stream and shop): an hourglass.</summary>
        IconTimer = 36,
        /// <summary>The mute-emotes button on each player in the Players panel.</summary>
        IconMute = 37,
        /// <summary>The shop's Remove a Viewer button.</summary>
        IconRemoveViewer = 38,
        // The rounded frames around the menu bar's icons (one entry each, so each can have its own art and colour).
        /// <summary>The frame around the stream counter icon (left of the menu bar).</summary>
        FrameNight = 39,
        /// <summary>The frame around the Clout icon in the menu bar.</summary>
        FramePop = 40,
        /// <summary>The frame around the Cash icon in the menu bar.</summary>
        FrameCash = 41,
        /// <summary>The frame around the Trophies icon in the menu bar.</summary>
        FrameStars = 42,
        /// <summary>The frame of the Audience button in the menu bar (its border).</summary>
        FrameAudience = 43,
        /// <summary>The frame of the Menu button in the menu bar (its border).</summary>
        FrameMenu = 44,
        /// <summary>The rounded rectangle under the Add Viewer button (the button overlaps its top).</summary>
        AddViewerBase = 45,
        /// <summary>The rounded rectangle under the End the Stream button (the button overlaps its top).</summary>
        EndStreamBase = 46,
    }

    [Serializable]
    public sealed class SkinEntry
    {
        [HideInInspector] public string name;
        public SkinSlot slot;
        public Sprite sprite;
        [Tooltip("Sliced for 9-slice frames, Simple for icons.")]
        public Image.Type imageType = Image.Type.Sliced;
        [Tooltip("Scales the 9-slice border thickness.")]
        public float pixelsPerUnitMultiplier = 1f;
        [Tooltip("When ticked the colour below replaces the scene colour (use white to show the sprite's own colours).")]
        public bool overrideColor;
        public Color color = Color.white;
    }

    /// <summary>
    /// The colour of one text element of the stream and shop (UI Skin > Text Colours). The scene builders list every text
    /// they make (Deck Party > Setup > Rebuild Scenes and UI Prefabs); a text follows its entry through
    /// <see cref="DeckParty.UI.SkinnedText"/>.
    /// </summary>
    [Serializable]
    public sealed class TextColourEntry
    {
        [Tooltip("The text's place in the scene or prefab (kept when you rebuild, so your colour stays on it).")]
        public string id;
        [Tooltip("A readable name for the text.")]
        public string label;
        [Tooltip("The part of the game it's in (menu bar, stream, shop...).")]
        public string group;
        [Tooltip("What the text said when the scene was built (the game rewrites most texts while playing).")]
        public string sample;
        [Tooltip("What made it: the Game scene, a UI prefab, or the game's code.")]
        public string source;
        [Tooltip("The last rebuild no longer made this text (kept because it has a colour of its own).")]
        public bool missing;
        [Tooltip("Ticked: the text uses the colour below instead of the game's colour.")]
        public bool overrideColor;
        public Color color = Color.black;
    }

    /// <summary>
    /// Central UI art. Drop sprites from your UI sprite sheet into the slots (or run
    /// Deck Party > Art > Auto-Assign Sprites) and every screen picks them up. The inspector (Editor/UISkinEditor.cs) lists
    /// every entry by name with where it shows.
    /// </summary>
    [CreateAssetMenu(menuName = "Deck Party/UI Skin", fileName = "UISkin")]
    public sealed class UISkin : ScriptableObject
    {
        [Tooltip("Optional TextMeshPro font used for all body text.")]
        public TMP_FontAsset bodyFont;
        [Tooltip("Optional TextMeshPro font for titles and big numbers.")]
        public TMP_FontAsset headerFont;

        public List<SkinEntry> entries = new List<SkinEntry>();

        [Tooltip("The colours of the website look (stream, shop, light pop-ups): page, panels, borders, dividers and text, " +
                 "shared or per panel. Applied live; kept when the scenes are rebuilt.")]
        public SiteColourSet siteColours = new SiteColourSet();

        [Tooltip("A colour for each text of the stream and shop. The list is filled when the scenes are rebuilt; tick an entry " +
                 "to give that text its own colour.")]
        public List<TextColourEntry> textColours = new List<TextColourEntry>();

        [NonSerialized] Dictionary<SkinSlot, SkinEntry> m_Lookup;
        [NonSerialized] Dictionary<string, TextColourEntry> m_TextLookup;

        public static event Action SkinChanged;

        public SkinEntry Get(SkinSlot slot)
        {
            if (m_Lookup == null)
            {
                m_Lookup = new Dictionary<SkinSlot, SkinEntry>();
                foreach (var entry in entries)
                    if (entry != null) m_Lookup[entry.slot] = entry;
            }
            m_Lookup.TryGetValue(slot, out var result);
            return result;
        }

        public Sprite GetSprite(SkinSlot slot) => Get(slot)?.sprite;

        /// <summary>The text colour entry with this id, or null.</summary>
        public TextColourEntry GetText(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            if (m_TextLookup == null)
            {
                m_TextLookup = new Dictionary<string, TextColourEntry>();
                if (textColours != null)
                    foreach (var entry in textColours)
                        if (entry != null && !string.IsNullOrEmpty(entry.id)) m_TextLookup[entry.id] = entry;
            }
            m_TextLookup.TryGetValue(id, out var result);
            return result;
        }

        /// <summary>The colour this text should have, when its entry overrides the game's colour.</summary>
        public bool TryGetTextColour(string id, out Color color)
        {
            var entry = GetText(id);
            if (entry != null && entry.overrideColor)
            {
                color = entry.color;
                return true;
            }
            color = default;
            return false;
        }

        /// <summary>Adds any missing slots so the inspector always lists every slot.</summary>
        public void EnsureAllSlots()
        {
            foreach (SkinSlot slot in Enum.GetValues(typeof(SkinSlot)))
            {
                var existing = entries.Find(e => e != null && e.slot == slot);
                if (existing == null) entries.Add(existing = new SkinEntry { slot = slot });
                existing.name = slot.ToString();
            }
            entries.Sort((a, b) => a.slot.CompareTo(b.slot));
            if (siteColours == null) siteColours = new SiteColourSet();
            siteColours.EnsureAllAreas();
            if (textColours == null) textColours = new List<TextColourEntry>();
            m_Lookup = null;
            m_TextLookup = null;
        }

        /// <summary>Tells every skinned graphic to re-apply (after a change made from code rather than the inspector).</summary>
        public void NotifyChanged()
        {
            m_Lookup = null;
            m_TextLookup = null;
            SkinChanged?.Invoke();
        }

        void OnValidate()
        {
            foreach (var entry in entries)
                if (entry != null) entry.name = entry.slot.ToString();
            m_Lookup = null;
            m_TextLookup = null;
            SkinChanged?.Invoke();
        }
    }
}
