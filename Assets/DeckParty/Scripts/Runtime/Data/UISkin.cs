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

        [NonSerialized] Dictionary<SkinSlot, SkinEntry> m_Lookup;

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
            m_Lookup = null;
        }

        /// <summary>Tells every skinned graphic to re-apply (after a change made from code rather than the inspector).</summary>
        public void NotifyChanged()
        {
            m_Lookup = null;
            SkinChanged?.Invoke();
        }

        void OnValidate()
        {
            foreach (var entry in entries)
                if (entry != null) entry.name = entry.slot.ToString();
            m_Lookup = null;
            SkinChanged?.Invoke();
        }
    }
}
