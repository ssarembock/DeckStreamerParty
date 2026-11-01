using System;
using DeckParty.Core;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace DeckParty.UI
{
    /// <summary>
    /// One viewer for sale in the shop grid: a big picture, the name, the group icon, the price and how many are left.
    /// Everything else (stats, ability, how many you own) is in the hover tooltip or the Audience [R]. Click to buy.
    /// "Left" is how many more this player can recruit: the shop's stock is shared by every player in the match, and
    /// non-Diamond viewers are also capped per player.
    /// </summary>
    public sealed class ShopItemView : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [SerializeField] internal Button button;
        [SerializeField] internal Image background;
        [SerializeField] internal Image frame;
        [SerializeField] internal RectTransform animRoot;
        [SerializeField] internal Image portrait;
        [SerializeField] internal Image placeholder;
        [SerializeField] internal TMP_Text initialsLabel;
        [SerializeField] internal TMP_Text nameLabel;
        [SerializeField] internal Image groupIcon;
        [SerializeField] internal GameObject diamondBadge;
        [SerializeField] internal TMP_Text priceLabel;
        [SerializeField] internal TMP_Text leftLabel;
        [SerializeField] internal Image dim;
        [SerializeField] internal TMP_Text soldOutLabel;
        [SerializeField] internal TooltipTrigger tooltip;

        public GuestType Type { get; private set; }
        public event Action<GuestType> BuyClicked;
        /// <summary>The pointer entered (true) or left (false). Used for multiplayer presence signals.</summary>
        public event Action<ShopItemView, bool> Hovered;

        public RectTransform PortraitRect => portrait != null ? (RectTransform)portrait.transform.parent : (RectTransform)transform;
        public RectTransform AnimRoot => animRoot != null ? animRoot : (RectTransform)transform;

        bool m_Hooked;
        CharacterPortrait m_Character;

        void Awake() => Hook();

        void OnDisable()
        {
            if (animRoot != null)
            {
                animRoot.localScale = Vector3.one;
                animRoot.localRotation = Quaternion.identity;
            }
        }

        void Hook()
        {
            if (m_Hooked) return;
            m_Hooked = true;
            if (button != null) button.onClick.AddListener(() => BuyClicked?.Invoke(Type));
            if (portrait != null)
            {
                m_Character = portrait.GetComponent<CharacterPortrait>();
                if (m_Character == null)
                {
                    m_Character = portrait.gameObject.AddComponent<CharacterPortrait>();
                    m_Character.image = portrait;
                }
            }
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            Hovered?.Invoke(this, true);
            AudioService.PlayHoverTick(transform.GetSiblingIndex());
        }

        public void OnPointerExit(PointerEventData eventData) => Hovered?.Invoke(this, false);

        /// <param name="state">The player whose shop this is (their wallet, collection and copies left).</param>
        /// <param name="blocker">Why it can't be bought right now (already localized), or null.</param>
        public void Bind(GuestType type, PlayerState state, string blocker)
        {
            Hook();
            Type = type;
            var info = GuestCatalog.Get(type);

            var sprite = GuestVisuals.Portrait(type);
            var art = GameDatabase.Instance.GetGuestArt(type);
            if (m_Character != null) m_Character.ShowViewer(type, info.HasAbility ? CharacterState.IdlePreActive : CharacterState.Idle);
            if (placeholder != null)
            {
                placeholder.gameObject.SetActive(sprite == null && (art == null || !art.IsAnimated));
                placeholder.color = GuestVisuals.Tint(type);
            }
            if (initialsLabel != null) initialsLabel.text = GuestVisuals.Initials(type);
            if (nameLabel != null) nameLabel.text = GuestVisuals.DisplayName(type);

            var icon = GuestVisuals.FamilyIcon(info.Category);
            if (groupIcon != null)
            {
                groupIcon.gameObject.SetActive(icon != null);
                groupIcon.sprite = icon;
                groupIcon.preserveAspect = true;
            }
            if (diamondBadge != null) diamondBadge.SetActive(info.IsDiamond);
            if (frame != null) frame.color = info.IsDiamond ? Color.Lerp(UIPalette.Border, UIPalette.Gold, 0.75f) : UIPalette.Border;

            bool affordable = state.Pop >= info.Cost;
            if (priceLabel != null)
            {
                priceLabel.text = Loc.Get("fmt.gifted_subs", "value", info.Cost, "n", info.Cost);
                priceLabel.color = affordable ? UIPalette.Gold : UIPalette.Red;
            }

            int left = PartyEngine.ShopLeft(state, type);
            bool soldOut = left <= 0;
            if (leftLabel != null)
            {
                leftLabel.text = left == int.MaxValue ? Loc.Get("shop.left_unlimited") : Loc.Get("shop.left", "count", left);
                leftLabel.color = soldOut ? UIPalette.Red : left == 1 ? UIPalette.Orange : UIPalette.TextDim;
            }

            bool canBuy = blocker == null;
            if (button != null) button.interactable = canBuy;
            if (dim != null) dim.enabled = !canBuy;
            if (soldOutLabel != null)
            {
                soldOutLabel.gameObject.SetActive(soldOut);
                soldOutLabel.text = Loc.Get("shop.sold_out");
            }

            if (tooltip != null)
            {
                var shopState = state;
                string why = blocker;
                tooltip.Provider = () =>
                {
                    string text = GuestVisuals.Tooltip(type, null, null, shopState);
                    return why != null ? text + "\n" + GuestVisuals.Colored(why, UIPalette.Orange) : text;
                };
            }
        }
    }
}
