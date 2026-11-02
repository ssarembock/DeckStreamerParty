using DeckParty.Core;
using UnityEngine;
using UnityEngine.UI;

namespace DeckParty.UI
{
    /// <summary>
    /// A viewer's community icons (on cards and shop tiles). Each icon explains its community on hover, above the card's own
    /// tooltip. Shows up to as many communities as there are icon slots; the tooltip lists them all.
    /// </summary>
    public sealed class CommunityIconsView : MonoBehaviour
    {
        [SerializeField] internal Image[] icons;
        [SerializeField] internal TooltipTrigger[] tooltips;

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
            for (; slot < icons.Length; slot++)
                if (icons[slot] != null) icons[slot].gameObject.SetActive(false);
        }

        public void Clear()
        {
            if (icons == null) return;
            foreach (var icon in icons)
                if (icon != null) icon.gameObject.SetActive(false);
        }

        void Show(int slot, Community community)
        {
            var icon = icons[slot];
            if (icon == null) return;
            icon.gameObject.SetActive(true);
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
