using System;
using DeckParty.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DeckParty.UI
{
    /// <summary>A wide guest row used by the shop (with Buy button) and the guest glossary (without).</summary>
    public sealed class GuestRowView : MonoBehaviour
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

        bool m_Hooked;

        void Awake() => Hook();

        void Hook()
        {
            if (m_Hooked) return;
            m_Hooked = true;
            if (buyButton != null) buyButton.onClick.AddListener(() => BuyClicked?.Invoke(Type));
        }

        public void Bind(GuestType type, bool shopMode)
        {
            Hook();
            Type = type;
            var info = GuestCatalog.Get(type);

            var sprite = GuestVisuals.Portrait(type);
            portrait.enabled = sprite != null;
            portrait.sprite = sprite;
            placeholder.gameObject.SetActive(sprite == null);
            placeholder.color = GuestVisuals.Tint(type);
            initialsLabel.text = GuestVisuals.Initials(type);

            nameLabel.text = GuestVisuals.DisplayName(type);
            if (tagsLabel != null) tagsLabel.text = GuestVisuals.Tags(info);
            descriptionLabel.text = info.Description;
            statsLabel.text = GuestVisuals.BaseStats(info);
            costLabel.enableAutoSizing = true;
            costLabel.fontSizeMin = 14;
            costLabel.text = info.Purchasable ? $"{info.Cost} {Terms.GiftedSubs}" : "Starter";
            if (starTag != null) starTag.SetActive(info.IsDiamond);

            buyButton.gameObject.SetActive(shopMode);
            ownedLabel.gameObject.SetActive(shopMode);
            if (tooltip != null) tooltip.Provider = () => GuestVisuals.Tooltip(type);
        }

        public void SetShopState(int owned, int max, string blocker)
        {
            ownedLabel.text = max == int.MaxValue ? $"Owned {owned}" : $"Owned {owned}/{max}";
            bool canBuy = blocker == null;
            buyButton.interactable = canBuy;
            buyLabel.text = canBuy ? "Buy" : (owned >= max ? "Maxed" : "Buy");
            if (buyTooltip != null) buyTooltip.Text = blocker;
        }
    }
}
