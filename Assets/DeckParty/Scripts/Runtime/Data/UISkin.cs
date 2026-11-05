using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DeckParty
{
    /// <summary>Named UI art slots. Every skinned Image in the scenes and prefabs points at one of these.</summary>
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
    /// Deck Party > Art > Auto-Assign Sprites) and every screen picks them up.
    /// </summary>
    [CreateAssetMenu(menuName = "Deck Party/UI Skin", fileName = "UISkin")]
    public sealed class UISkin : ScriptableObject
    {
        [Tooltip("Optional TextMeshPro font used for all body text.")]
        public TMP_FontAsset bodyFont;
        [Tooltip("Optional TextMeshPro font for titles and big numbers.")]
        public TMP_FontAsset headerFont;

        public List<SkinEntry> entries = new List<SkinEntry>();

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
            m_Lookup = null;
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
