using System;
using DeckParty.Core;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace DeckParty.UI
{
    /// <summary>A wide viewer row used by the shop (with Buy button) and the viewer glossary (without).</summary>
    public sealed class GuestRowView : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [SerializeField] internal Image background;
        [SerializeField] internal Image portrait;
        [SerializeField] internal Image placeholder;
        [SerializeField] internal TMP_Text initialsLabel;
        [SerializeField] internal TMP_Text nameLabel;
        [SerializeField] internal TMP_Text tagsLabel;
        [SerializeField] internal TMP_Text descriptionLabel;
        [SerializeField] internal TMP_Text statsLabel;
        [SerializeField] internal TMP_Text costLabel;
        [SerializeField] internal TMP_Text ownedLabel;
        [SerializeField] internal GameObject starTag;
        [SerializeField] internal Button buyButton;
        [SerializeField] internal TMP_Text buyLabel;
        [SerializeField] internal TooltipTrigger buyTooltip;
        [SerializeField] internal TooltipTrigger tooltip;

        public GuestType Type { get; private set; }
        public event Action<GuestType> BuyClicked;
        /// <summary>The pointer entered (true) or left (false) this row. Used for multiplayer presence signals.</summary>
        public event Action<GuestRowView, bool> Hovered;

        public RectTransform PortraitRect => portrait != null ? (RectTransform)portrait.transform.parent : (RectTransform)transform;

        bool m_Hooked;

        void Awake() => Hook();

        void Hook()
        {
            if (m_Hooked) return;
            m_Hooked = true;
            if (buyButton != null) buyButton.onClick.AddListener(() => BuyClicked?.Invoke(Type));
        }

        public void OnPointerEnter(PointerEventData eventData) => Hovered?.Invoke(this, true);
        public void OnPointerExit(PointerEventData eventData) => Hovered?.Invoke(this, false);

        public void Bind(GuestType type, bool shopMode)
        {
            Hook();
            Type = type;
            var info = GuestCatalog.Get(type);

            var sprite = GuestVisuals.Portrait(type);
            var art = GameDatabase.Instance.GetGuestArt(type);
            var character = portrait.GetComponent<CharacterPortrait>();
            if (character == null)
            {
                character = portrait.gameObject.AddComponent<CharacterPortrait>();
                character.image = portrait;
            }
            // Shop, Audience and glossary rest in Idle_Pre_Active for viewers with an action.
            character.ShowViewer(type, info.HasAbility ? CharacterState.IdlePreActive : CharacterState.Idle);
            placeholder.gameObject.SetActive(sprite == null && (art == null || !art.IsAnimated));
            placeholder.color = GuestVisuals.Tint(type);
            initialsLabel.text = GuestVisuals.Initials(type);

            // The viewer's community icons follow the name (hover one to see what it does).
            string icons = CommunityVisuals.Icons(type);
            nameLabel.text = icons.Length > 0 ? GuestVisuals.DisplayName(type) + "  " + icons : GuestVisuals.DisplayName(type);
            if (tagsLabel != null) tagsLabel.text = GuestVisuals.Tags(info);
            descriptionLabel.text = GuestVisuals.Description(type);
            statsLabel.text = GuestVisuals.BaseStats(info);
            costLabel.enableAutoSizing = true;
            costLabel.fontSizeMin = 14;
            costLabel.text = info.Purchasable ? GameIcons.Amount(ResourceIcon.Subs, info.Cost) : Loc.Get("shop.starter");
            if (starTag != null) starTag.SetActive(info.IsDiamond);

            buyButton.gameObject.SetActive(shopMode);
            ownedLabel.gameObject.SetActive(shopMode);
            if (tooltip != null) tooltip.Provider = () => GuestVisuals.Tooltip(type);
        }

        /// <param name="blocker">Why it can't be bought (already localized), or null.</param>
        public void SetShopState(int owned, int max, string blocker)
        {
            ownedLabel.text = max == int.MaxValue ? Loc.Get("shop.owned", "count", owned) : Loc.Get("shop.owned_max", "count", owned, "max", max);
            bool canBuy = blocker == null;
            buyButton.interactable = canBuy;
            buyLabel.text = Loc.Get(!canBuy && owned >= max ? "shop.maxed" : "shop.buy");
            if (buyTooltip != null) buyTooltip.Text = blocker;
        }
    }
}
