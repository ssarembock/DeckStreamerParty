using DeckParty.Core;
using UnityEngine;
using UnityEngine.UI;

namespace DeckParty.UI
{
    /// <summary>
    /// A viewer's community icons (on cards and shop tiles). Each icon explains its community on hover, above the card's own
    /// tooltip. Shows up to as many communities as there are icon slots; the tooltip lists them all.
    /// <para>With <see cref="arc"/> on (the chat avatars) each icon sits in its own small black circle, and the circles sit
    /// along the bottom of the avatar's ring: one community is centred at the bottom, two or three fan out evenly from there.
    /// The view's RectTransform must be centred on the ring.</para>
    /// </summary>
    public sealed class CommunityIconsView : MonoBehaviour
    {
        [SerializeField] internal Image[] icons;
        [SerializeField] internal TooltipTrigger[] tooltips;

        [Header("Arc layout (chat avatars)")]
        [Tooltip("Lay the icons out along the bottom of a circle centred on this object.")]
        [SerializeField] internal bool arc;
        [Tooltip("Distance from the circle's centre to each icon's centre, in pixels.")]
        [SerializeField] internal float arcRadius = 66f;
        [Tooltip("Angle between neighbouring icons, in degrees.")]
        [SerializeField] internal float arcStep = 28f;
        [Tooltip("The black circle behind each icon (one per icon slot); shown and placed together with its icon.")]
        [SerializeField] internal RectTransform[] badges;

        public void Bind(GuestType type)
        {
            if (icons == null) return;
            var mask = GuestCatalog.CommunitiesOf(type);
            int slot = 0;
            foreach (var community in Communities.All)
            {
                if (!Communities.Has(mask, community)) continue;
                if (slot >= icons.Length) break;
                Show(slot++, community);
            }
            int shown = slot;
            for (; slot < icons.Length; slot++) SetVisible(slot, false);
            if (arc) Arrange(shown);
        }

        public void Clear()
        {
            if (icons == null) return;
            for (int i = 0; i < icons.Length; i++) SetVisible(i, false);
        }

        /// <summary>Centres <paramref name="count"/> icons on the bottom of the circle, evenly spaced.</summary>
        void Arrange(int count)
        {
            for (int i = 0; i < count; i++)
            {
                var target = Badge(i) ?? (icons[i] != null ? icons[i].rectTransform : null);
                if (target == null) continue;
                float angle = (-90f + (i - (count - 1) * 0.5f) * arcStep) * Mathf.Deg2Rad;
                target.anchorMin = target.anchorMax = new Vector2(0.5f, 0.5f);
                target.pivot = new Vector2(0.5f, 0.5f);
                target.anchoredPosition = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * arcRadius;
            }
        }

        RectTransform Badge(int slot) => badges != null && slot < badges.Length ? badges[slot] : null;

        void SetVisible(int slot, bool visible)
        {
            var badge = Badge(slot);
            if (badge != null) badge.gameObject.SetActive(visible);
            if (icons[slot] != null) icons[slot].gameObject.SetActive(visible);
        }

        void Show(int slot, Community community)
        {
            var icon = icons[slot];
            if (icon == null) return;
            SetVisible(slot, true);
            icon.sprite = GameIcons.Sprite(community);
            icon.color = Color.white;
            icon.preserveAspect = true;
            icon.raycastTarget = true;

            var tooltip = tooltips != null && slot < tooltips.Length ? tooltips[slot] : null;
            if (tooltip == null) return;
            tooltip.Overlay = true;
            tooltip.Provider = () => CommunityVisuals.Tooltip(community, CommunityVisuals.LiveState);
        }
    }
}
