using System;
using DeckParty.Core;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace DeckParty.UI
{
    /// <summary>
    /// One viewer for sale in the shop grid: a big picture, the community icons, the price (icon and number) and how many are
    /// left ("x3"). The name and everything else (what it scores, ability, how many you own) is in the hover tooltip or the
    /// Audience [R]. Click to buy. Drawn as a white website card (the stream and shop use the light <see cref="SitePalette"/>).
    /// "Left" is how many more this player can recruit: the shop's stock is shared by every player in the match, and
    /// non-Plays viewers are also capped per player.
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
        [Tooltip("Old single group icon (replaced by Communities). Hidden when present.")]
        [SerializeField] internal Image groupIcon;
        [SerializeField] internal CommunityIconsView communities;
        [SerializeField] internal GameObject diamondBadge;
        [SerializeField] internal TMP_Text priceLabel;
        [SerializeField] internal TMP_Text leftLabel;
        [SerializeField] internal Image dim;
        [SerializeField] internal TMP_Text soldOutLabel;
        [Tooltip("Website look: an icon over the picture when none are left (instead of the SOLD OUT text).")]
        [SerializeField] internal GameObject soldOutBadge;
        [SerializeField] internal TooltipTrigger tooltip;
        [Tooltip("Light (website) colours for the frame and labels.")]
        [SerializeField] internal bool siteStyle;
        [Tooltip("Card backs peeking out behind the tile (up and to the left), nearest first: one shows for every copy left " +
                 "after the top one, so the pile shrinks as the shop runs out.")]
        [SerializeField] internal GameObject[] stack;

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

        /// <summary>
        /// The pile under the tile: one card back per copy left after the top card (Plays viewers never run out, so their pile
        /// is always full). The back that disappears after a purchase is the farthest one, so the pile visibly shrinks.
        /// </summary>
        void SetStack(int left)
        {
            if (stack == null) return;
            int backs = left == int.MaxValue ? stack.Length : Mathf.Clamp(left - 1, 0, stack.Length);
            for (int i = 0; i < stack.Length; i++)
                if (stack[i] != null && stack[i].activeSelf != (i < backs)) stack[i].SetActive(i < backs);
        }

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

            if (groupIcon != null) groupIcon.gameObject.SetActive(false);
            if (communities != null) communities.Bind(type);
            if (diamondBadge != null) diamondBadge.SetActive(info.IsDiamond);
            var border = siteStyle ? SitePalette.Outline : UIPalette.Border;
            if (frame != null) frame.color = info.IsDiamond ? Color.Lerp(border, siteStyle ? SitePalette.Gold : UIPalette.Gold, 0.75f) : border;

            bool affordable = state.Pop >= info.Cost;
            if (priceLabel != null)
            {
                // The icon and the number only: hovering the icon names the resource.
                priceLabel.text = $"<size=125%>{GameIcons.Tag(ResourceIcon.Subs)}</size> {info.Cost}";
                priceLabel.color = affordable ? (siteStyle ? SitePalette.Text : UIPalette.Gold) : (siteStyle ? SitePalette.Danger : UIPalette.Red);
            }

            int left = PartyEngine.ShopLeft(state, type);
            bool soldOut = left <= 0;
            SetStack(left);
            if (leftLabel != null)
            {
                if (siteStyle) leftLabel.text = left == int.MaxValue ? string.Empty : "×" + Mathf.Max(0, left);
                else leftLabel.text = left == int.MaxValue ? Loc.Get("shop.left_unlimited") : Loc.Get("shop.left", "count", left);
                leftLabel.color = soldOut ? (siteStyle ? SitePalette.Danger : UIPalette.Red)
                    : left == 1 ? (siteStyle ? SitePalette.Warning : UIPalette.Orange)
                    : siteStyle ? SitePalette.TextDim : UIPalette.TextDim;
            }

            bool canBuy = blocker == null;
            if (button != null) button.interactable = canBuy;
            if (dim != null) dim.enabled = !canBuy;
            if (soldOutLabel != null)
            {
                soldOutLabel.gameObject.SetActive(soldOut);
                soldOutLabel.text = Loc.Get("shop.sold_out");
            }
            if (soldOutBadge != null) soldOutBadge.SetActive(soldOut);

            if (tooltip != null)
            {
                var shopState = state;
                string why = blocker;
                int copies = left;
                tooltip.Provider = () =>
                {
                    string text = GuestVisuals.Tooltip(type, null, null, shopState);
                    // The tile shows "x3": the tooltip says what that means.
                    if (siteStyle) text += "\n" + GuestVisuals.Colored(copies <= 0 ? Loc.Get("shop.sold_out")
                        : copies == int.MaxValue ? Loc.Get("shop.left_unlimited") : Loc.Get("shop.left", "count", copies), UIPalette.TextDim);
                    return why != null ? text + "\n" + GuestVisuals.Colored(why, UIPalette.Orange) : text;
                };
            }
        }
    }
}
