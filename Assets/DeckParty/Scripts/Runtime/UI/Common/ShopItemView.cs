using System;
using System.Collections.Generic;
using DeckParty.Core;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
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
    /// <para>Your own card prefab (UI Skin > Shop > Card prefab) works too: <see cref="AutoWire"/> finds its parts by their
    /// <see cref="ShopCardPart"/> marker or name, and fills in the optional ones (name, description, stats...) when present.
    /// UI Skin > Shop also sets the card colours, piles and (on the built-in card) the shape.</para>
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
        [Tooltip("Card backs peeking out above the tile (straight up), nearest first: one shows for every copy left " +
                 "after the top one, so the pile shrinks as the shop runs out.")]
        [SerializeField] internal GameObject[] stack;

        [Header("Optional (your own card prefab)")]
        [SerializeField] internal TMP_Text descriptionLabel;
        [Tooltip("What the viewer scores, as icons and numbers.")]
        [SerializeField] internal TMP_Text statsLabel;
        [SerializeField] internal TMP_Text cashLabel;
        [SerializeField] internal TMP_Text influenceLabel;
        [Tooltip("The name of the viewer's action (empty when it has none).")]
        [SerializeField] internal TMP_Text actionLabel;
        [SerializeField] internal GameObject riskBadge;
        [Tooltip("Shown only on viewers that cancel Deplatform Risk (negative Risk: a shield).")]
        [SerializeField, FormerlySerializedAs("cancelsRiskBadge")] internal GameObject shieldBadge;
        [SerializeField] internal GameObject actionBadge;

        [Tooltip("The built-in card (set by the builder): UI Skin > Shop's card shape, border and pile step apply to it.")]
        [SerializeField] internal bool builtInCard;

        public GuestType Type { get; private set; }
        public event Action<GuestType> BuyClicked;
        /// <summary>The pointer entered (true) or left (false). Used for multiplayer presence signals.</summary>
        public event Action<ShopItemView, bool> Hovered;

        public RectTransform PortraitRect => portrait != null ? (RectTransform)portrait.transform.parent : (RectTransform)transform;
        public RectTransform AnimRoot => animRoot != null ? animRoot : (RectTransform)transform;

        Button m_HookedButton;
        CharacterPortrait m_Character;
        ButtonFeedback m_Feedback;

        // The built-in look, captured once so UI Skin > Shop can be cleared again while playing.
        bool m_Captured;
        Sprite m_FrameSprite, m_BackgroundSprite;
        float m_BorderWidth = 3f;
        Color m_Veil = new Color(1f, 1f, 1f, 0.62f);
        float m_HoverScale = 1.04f;
        Vector2[] m_StackOffsets;
        readonly List<(Image image, Sprite sprite)> m_PileImages = new List<(Image, Sprite)>();
        /// <summary>Inner outlines on the card that light up when the viewer has an action (found once, see <see cref="InnerOutline"/>).</summary>
        InnerOutline[] m_ActionOutlines;
        GuestType m_OutlineType;
        bool m_OutlineBound;

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
            if (button != null && m_HookedButton != button)
            {
                button.onClick.AddListener(() => BuyClicked?.Invoke(Type));
                m_HookedButton = button;
            }
            if (portrait != null && m_Character == null)
            {
                m_Character = portrait.GetComponent<CharacterPortrait>();
                if (m_Character == null)
                {
                    m_Character = portrait.gameObject.AddComponent<CharacterPortrait>();
                    m_Character.image = portrait;
                }
            }
            if (m_Feedback == null) m_Feedback = GetComponent<ButtonFeedback>();
            Capture();
        }

        void Capture()
        {
            if (m_Captured) return;
            m_Captured = true;
            if (frame != null) m_FrameSprite = frame.sprite;
            if (background != null)
            {
                m_BackgroundSprite = background.sprite;
                if (builtInCard) m_BorderWidth = background.rectTransform.offsetMin.x;
            }
            if (dim != null) m_Veil = dim.color;
            if (m_Feedback != null) m_HoverScale = m_Feedback.hoverScale;
            if (stack != null)
            {
                m_StackOffsets = new Vector2[stack.Length];
                for (int i = 0; i < stack.Length; i++)
                {
                    if (stack[i] == null) continue;
                    m_StackOffsets[i] = ((RectTransform)stack[i].transform).offsetMin;
                    foreach (var image in stack[i].GetComponentsInChildren<Image>(true)) m_PileImages.Add((image, image.sprite));
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
        void SetStack(int left, ShopLook look)
        {
            if (stack == null) return;
            int backs = left == int.MaxValue ? stack.Length : Mathf.Clamp(left - 1, 0, stack.Length);
            if (look != null && look.hidePiles) backs = 0;
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
            var look = ShopLook.Current;
            ApplyShape(look);

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
            if (descriptionLabel != null) descriptionLabel.text = GuestVisuals.Description(type);
            if (statsLabel != null) statsLabel.text = GuestVisuals.ScoringIcons(info);
            if (cashLabel != null) cashLabel.text = GuestVisuals.CashValue(info);
            if (influenceLabel != null) influenceLabel.text = GuestVisuals.InfluenceValue(info);
            if (actionLabel != null) actionLabel.text = info.HasAbility ? GuestVisuals.ActionName(type) : string.Empty;
            if (riskBadge != null) riskBadge.SetActive(info.Trouble);
            if (shieldBadge != null) shieldBadge.SetActive(info.CancelsTrouble);
            if (actionBadge != null) actionBadge.SetActive(info.HasAbility);
            ShowActionOutlines(type, info.HasAbility);

            if (groupIcon != null) groupIcon.gameObject.SetActive(false);
            if (communities != null) communities.Bind(type);
            if (diamondBadge != null) diamondBadge.SetActive(info.IsDiamond);
            var border = siteStyle ? SiteColours.Get(SiteColourRole.PanelBorder, transform) : UIPalette.Border;
            if (frame != null)
            {
                var trophy = Color.Lerp(border, siteStyle ? SitePalette.Gold : UIPalette.Gold, 0.75f);
                frame.color = info.IsDiamond ? (look != null ? look.trophyBorder.Or(trophy) : trophy) : border;
            }

            // Shop Control's discount / rival Saboteurs' surcharge on the next recruit: the old price struck through.
            int cost = PartyEngine.RecruitCost(state, type);
            bool affordable = state.Cash >= cost;
            if (priceLabel != null)
            {
                // The icon and the number only: hovering the icon names the resource.
                string was = cost != info.Cost ? $"<s><size=80%>{info.Cost}</size></s> " : string.Empty;
                priceLabel.text = $"<size=125%>{GameIcons.Tag(ResourceIcon.Cash)}</size> {was}{cost}";
                var canColour = siteStyle ? SiteColours.Get(SiteColourRole.Text, transform) : UIPalette.Gold;
                var cannotColour = siteStyle ? SitePalette.Danger : UIPalette.Red;
                priceLabel.color = affordable
                    ? (look != null ? look.price.Or(canColour) : canColour)
                    : (look != null ? look.priceTooExpensive.Or(cannotColour) : cannotColour);
            }

            int left = PartyEngine.ShopLeft(state, type);
            bool soldOut = left <= 0;
            SetStack(left, look);
            if (leftLabel != null)
            {
                if (siteStyle) leftLabel.text = left == int.MaxValue ? string.Empty : "×" + Mathf.Max(0, left);
                else leftLabel.text = left == int.MaxValue ? Loc.Get("shop.left_unlimited") : Loc.Get("shop.left", "count", left);
                var outColour = siteStyle ? SitePalette.Danger : UIPalette.Red;
                var lastColour = siteStyle ? SitePalette.Warning : UIPalette.Orange;
                var normalColour = siteStyle ? SiteColours.Get(SiteColourRole.SecondaryText, transform) : UIPalette.TextDim;
                leftLabel.color = soldOut ? (look != null ? look.soldOut.Or(outColour) : outColour)
                    : left == 1 ? (look != null ? look.lastCopy.Or(lastColour) : lastColour)
                    : look != null ? look.copiesLeft.Or(normalColour) : normalColour;
            }

            bool canBuy = blocker == null;
            if (button != null) button.interactable = canBuy;
            if (dim != null)
            {
                dim.enabled = !canBuy;
                dim.color = look != null ? look.cantBuyVeil.Or(m_Veil) : m_Veil;
            }
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

        /// <summary>
        /// Lights the card's inner outlines that are set to "Viewer has an action" (see <see cref="InnerOutline"/>) on viewers
        /// with an action. Straight away when the card shows a different viewer, with a fade otherwise.
        /// </summary>
        void ShowActionOutlines(GuestType type, bool hasAction)
        {
            if (m_ActionOutlines == null) m_ActionOutlines = GetComponentsInChildren<InnerOutline>(true);
            bool instant = !m_OutlineBound || type != m_OutlineType;
            m_OutlineBound = true;
            m_OutlineType = type;
            foreach (var outline in m_ActionOutlines)
                if (outline != null && outline.showWhen == InnerOutline.ShowWhen.ViewerHasAction) outline.SetShown(hasAction, instant);
        }

        /// <summary>UI Skin > Shop on this card: hover growth, and on the built-in card its shape, border and pile step.</summary>
        void ApplyShape(ShopLook look)
        {
            if (m_Feedback != null) m_Feedback.hoverScale = look != null && look.hoverScale > 0f ? look.hoverScale : m_HoverScale;
            if (!builtInCard) return;

            var shape = look != null ? look.cardShape : null;
            float width = look != null && look.cardBorderWidth >= 0f ? look.cardBorderWidth : m_BorderWidth;
            if (frame != null)
            {
                SetSprite(frame, shape != null ? shape : m_FrameSprite);
                frame.enabled = width > 0f;
            }
            if (background != null)
            {
                SetSprite(background, shape != null ? shape : m_BackgroundSprite);
                background.rectTransform.offsetMin = new Vector2(width, width);
                background.rectTransform.offsetMax = new Vector2(-width, -width);
            }
            foreach (var (image, sprite) in m_PileImages)
                if (image != null) SetSprite(image, shape != null ? shape : sprite);

            if (stack == null || m_StackOffsets == null) return;
            float step = look != null ? look.pileStep : 0f;
            for (int i = 0; i < stack.Length && i < m_StackOffsets.Length; i++)
            {
                if (stack[i] == null) continue;
                var offset = step > 0f ? new Vector2(0f, step * (i + 1)) : m_StackOffsets[i];
                var rect = (RectTransform)stack[i].transform;
                rect.offsetMin = offset;
                rect.offsetMax = offset;
            }
        }

        static void SetSprite(Image image, Sprite sprite)
        {
            if (image.sprite != sprite) image.sprite = sprite;
            image.type = sprite != null && sprite.border != Vector4.zero ? Image.Type.Sliced : Image.Type.Simple;
        }

        // ------------------------------------------------------------------ your own card prefab

        /// <summary>
        /// Wires up a card made from your own prefab: every field left empty is looked up among the card's
        /// <see cref="ShopCardPart"/> markers, then by object name (the part's name, like "Price"). Adds what a card needs to
        /// work (a Button, the hover feedback and tooltip) if the prefab has none.
        /// </summary>
        public void AutoWire()
        {
            var parts = GetComponentsInChildren<ShopCardPart>(true);
            var all = GetComponentsInChildren<Transform>(true);

            if (background == null) background = Find<Image>(parts, all, ShopCardPart.Part.Background);
            if (background == null) background = GetComponent<Image>();
            if (frame == null) frame = Find<Image>(parts, all, ShopCardPart.Part.Frame);
            if (portrait == null) portrait = Find<Image>(parts, all, ShopCardPart.Part.Portrait);
            if (placeholder == null) placeholder = Find<Image>(parts, all, ShopCardPart.Part.Placeholder);
            if (initialsLabel == null) initialsLabel = Find<TMP_Text>(parts, all, ShopCardPart.Part.Initials);
            if (nameLabel == null) nameLabel = Find<TMP_Text>(parts, all, ShopCardPart.Part.Name);
            if (descriptionLabel == null) descriptionLabel = Find<TMP_Text>(parts, all, ShopCardPart.Part.Description);
            if (statsLabel == null) statsLabel = Find<TMP_Text>(parts, all, ShopCardPart.Part.Stats);
            if (cashLabel == null) cashLabel = Find<TMP_Text>(parts, all, ShopCardPart.Part.Cash);
            if (influenceLabel == null) influenceLabel = Find<TMP_Text>(parts, all, ShopCardPart.Part.Influence);
            if (actionLabel == null) actionLabel = Find<TMP_Text>(parts, all, ShopCardPart.Part.Action);
            if (priceLabel == null) priceLabel = Find<TMP_Text>(parts, all, ShopCardPart.Part.Price);
            if (leftLabel == null) leftLabel = Find<TMP_Text>(parts, all, ShopCardPart.Part.CopiesLeft);
            if (dim == null) dim = Find<Image>(parts, all, ShopCardPart.Part.CantBuyVeil);
            if (diamondBadge == null) diamondBadge = FindObject(parts, all, ShopCardPart.Part.TrophyBadge);
            if (riskBadge == null) riskBadge = FindObject(parts, all, ShopCardPart.Part.RiskBadge);
            if (shieldBadge == null) shieldBadge = FindObject(parts, all, ShopCardPart.Part.ShieldBadge);
            if (actionBadge == null) actionBadge = FindObject(parts, all, ShopCardPart.Part.ActionBadge);
            if (soldOutBadge == null) soldOutBadge = FindObject(parts, all, ShopCardPart.Part.SoldOut);
            if (animRoot == null) animRoot = Find<RectTransform>(parts, all, ShopCardPart.Part.AnimRoot);
            if (animRoot == null) animRoot = (RectTransform)transform;
            if (communities == null) communities = FindCommunities(parts, all);
            if (stack == null || stack.Length == 0) stack = FindPile(parts, all);

            if (background != null) background.raycastTarget = true;
            if (dim != null) dim.raycastTarget = false;
            if (button == null && !TryGetComponent(out button)) button = gameObject.AddComponent<Button>();
            if (button.targetGraphic == null) button.targetGraphic = background;
            button.transition = Selectable.Transition.None;
            button.navigation = new Navigation { mode = Navigation.Mode.None };
            if (!TryGetComponent(out m_Feedback)) m_Feedback = gameObject.AddComponent<ButtonFeedback>();
            if (tooltip == null && !TryGetComponent(out tooltip)) tooltip = gameObject.AddComponent<TooltipTrigger>();
            siteStyle = true;
            builtInCard = false;

            // The fields are set now: hook them up (Awake ran before, when nothing was wired yet).
            m_Captured = false;
            m_PileImages.Clear();
            m_ActionOutlines = null;
            Hook();
        }

        /// <summary>
        /// For the UI Skin inspector: which parts a card prefab has, found the same way <see cref="AutoWire"/> finds them (or
        /// already set on a Shop Item View the prefab carries).
        /// </summary>
        public static Dictionary<ShopCardPart.Part, bool> FindParts(GameObject prefab)
        {
            var result = new Dictionary<ShopCardPart.Part, bool>();
            if (prefab == null) return result;
            var parts = prefab.GetComponentsInChildren<ShopCardPart>(true);
            var all = prefab.GetComponentsInChildren<Transform>(true);
            foreach (ShopCardPart.Part part in Enum.GetValues(typeof(ShopCardPart.Part)))
            {
                bool found = part == ShopCardPart.Part.PileCard
                    ? FindPile(parts, all).Length > 0
                    : Find(parts, all, part, PartComponent(part)) != null;
                result[part] = found;
            }
            if (prefab.GetComponent<Image>() != null) result[ShopCardPart.Part.Background] = true;
            if (prefab.TryGetComponent<ShopItemView>(out var view))
            {
                if (view.background != null) result[ShopCardPart.Part.Background] = true;
                if (view.portrait != null) result[ShopCardPart.Part.Portrait] = true;
                if (view.priceLabel != null) result[ShopCardPart.Part.Price] = true;
                if (view.nameLabel != null) result[ShopCardPart.Part.Name] = true;
            }
            return result;
        }

        /// <summary>Other names an object may have for a part (besides the part's own name).</summary>
        static readonly Dictionary<ShopCardPart.Part, string[]> s_Aliases = new Dictionary<ShopCardPart.Part, string[]>
        {
            { ShopCardPart.Part.Price, new[] { "Cost" } },
            { ShopCardPart.Part.CopiesLeft, new[] { "Left", "Copies" } },
            { ShopCardPart.Part.TrophyBadge, new[] { "Trophy", "DiamondBadge" } },
            { ShopCardPart.Part.CantBuyVeil, new[] { "Veil", "Dim" } },
            { ShopCardPart.Part.Portrait, new[] { "Art" } },
            { ShopCardPart.Part.Name, new[] { "ViewerName" } },
            // Parts renamed on 2026-11-15: objects still named after the old resource names count.
            { ShopCardPart.Part.Cash, new[] { "Clout" } },
            { ShopCardPart.Part.ShieldBadge, new[] { "Shield", "CancelsRiskBadge" } },
        };

        static bool NameMatches(string objectName, ShopCardPart.Part part)
        {
            if (string.Equals(objectName, part.ToString(), StringComparison.OrdinalIgnoreCase)) return true;
            if (!s_Aliases.TryGetValue(part, out var aliases)) return false;
            foreach (var alias in aliases)
                if (string.Equals(objectName, alias, StringComparison.OrdinalIgnoreCase)) return true;
            return false;
        }

        /// <summary>The component a part must have to be used (a "Stats" object with no text in it isn't the Stats part).</summary>
        static Type PartComponent(ShopCardPart.Part part)
        {
            switch (part)
            {
                case ShopCardPart.Part.Initials:
                case ShopCardPart.Part.Name:
                case ShopCardPart.Part.Description:
                case ShopCardPart.Part.Stats:
                case ShopCardPart.Part.Cash:
                case ShopCardPart.Part.Influence:
                case ShopCardPart.Part.Action:
                case ShopCardPart.Part.Price:
                case ShopCardPart.Part.CopiesLeft:
                    return typeof(TMP_Text);
                case ShopCardPart.Part.Background:
                case ShopCardPart.Part.Frame:
                case ShopCardPart.Part.Portrait:
                case ShopCardPart.Part.Placeholder:
                case ShopCardPart.Part.CantBuyVeil:
                    return typeof(Image);
                default:
                    return typeof(Transform);
            }
        }

        /// <summary>For editor tools: a part in a card (or card prefab), found the same way <see cref="AutoWire"/> finds it.</summary>
        internal static T FindPart<T>(GameObject root, ShopCardPart.Part part) where T : Component =>
            root == null ? null : Find<T>(root.GetComponentsInChildren<ShopCardPart>(true), root.GetComponentsInChildren<Transform>(true), part);

        static T Find<T>(ShopCardPart[] parts, Transform[] all, ShopCardPart.Part part) where T : Component =>
            (T)Find(parts, all, part, typeof(T));

        static Component Find(ShopCardPart[] parts, Transform[] all, ShopCardPart.Part part, Type type)
        {
            foreach (var marker in parts)
                if (marker.Kind == part && marker.TryGetComponent(type, out var found)) return found;
            foreach (var t in all)
                if (NameMatches(t.name, part) && t.GetComponent<ShopCardPart>() == null && t.TryGetComponent(type, out var found)) return found;
            return null;
        }

        static GameObject FindObject(ShopCardPart[] parts, Transform[] all, ShopCardPart.Part part)
        {
            var found = Find<Transform>(parts, all, part);
            return found != null ? found.gameObject : null;
        }

        /// <summary>The Communities part: its own CommunityIconsView, or one made from its child Images (in order).</summary>
        static CommunityIconsView FindCommunities(ShopCardPart[] parts, Transform[] all)
        {
            var holder = Find<Transform>(parts, all, ShopCardPart.Part.Communities);
            if (holder == null) return null;
            if (holder.TryGetComponent<CommunityIconsView>(out var view)) return view;
            var icons = new List<Image>();
            var tips = new List<TooltipTrigger>();
            foreach (Transform child in holder)
            {
                if (!child.TryGetComponent<Image>(out var image)) continue;
                icons.Add(image);
                if (!child.TryGetComponent<TooltipTrigger>(out var tip)) tip = child.gameObject.AddComponent<TooltipTrigger>();
                tips.Add(tip);
            }
            view = holder.gameObject.AddComponent<CommunityIconsView>();
            view.icons = icons.ToArray();
            view.tooltips = tips.ToArray();
            return view;
        }

        /// <summary>The PileCard parts, nearest first (by Order), then any objects named PileCard1, PileCard2...</summary>
        static GameObject[] FindPile(ShopCardPart[] parts, Transform[] all)
        {
            var found = new List<(int order, GameObject card)>();
            foreach (var marker in parts)
                if (marker.Kind == ShopCardPart.Part.PileCard) found.Add((marker.Order, marker.gameObject));
            if (found.Count == 0)
                foreach (var t in all)
                    if (t.name.StartsWith("PileCard", StringComparison.OrdinalIgnoreCase) && int.TryParse(t.name.Substring(8), out int n))
                        found.Add((n, t.gameObject));
            found.Sort((a, b) => a.order.CompareTo(b.order));
            var result = new GameObject[found.Count];
            for (int i = 0; i < found.Count; i++) result[i] = found[i].card;
            return result;
        }
    }
}
