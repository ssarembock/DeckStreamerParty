using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DeckParty.UI
{
    /// <summary>
    /// Hover tooltip that follows the pointer and stays on screen. Two layers: the base tooltip (cards, buttons) and an
    /// override for small things inside them (a community icon on a card, an icon in a line of text). Leaving the small
    /// thing brings the base tooltip back.
    /// The card is sized to its text, up to <see cref="maxWidth"/>. Text built with <see cref="TipText"/> has a title row:
    /// it goes in <see cref="header"/>, above <see cref="divider"/>, and the rest goes in <see cref="label"/>.
    /// </summary>
    public sealed class TooltipView : MonoBehaviour
    {
        [SerializeField] internal RectTransform panel;
        [Tooltip("The body text (everything below the divider).")]
        [SerializeField] internal TMP_Text label;
        [SerializeField] internal CanvasGroup group;
        [Tooltip("Optional: the title row. Scenes built before 2026-11-16 have none, so the title goes on the body's first line.")]
        [SerializeField] internal TMP_Text header;
        [Tooltip("Optional: the line between the title and the body.")]
        [SerializeField] internal Graphic divider;
        [Tooltip("The widest the pop-up gets, in pixels (short text makes it narrower). 0 keeps the width it was built with.")]
        [SerializeField] internal float maxWidth = 440f;
        [Tooltip("The narrowest the pop-up gets, in pixels.")]
        [SerializeField] internal float minWidth = 96f;

        static TooltipView s_Active;
        RectTransform m_CanvasRect;
        LayoutGroup m_Layout;
        float m_ShowAt;
        string m_BaseText;
        object m_BaseOwner;
        string m_OverrideText;
        object m_OverrideOwner;
        string m_Shown;

        bool Visible => m_OverrideText != null || m_BaseText != null;

        void Awake()
        {
            s_Active = this;
            var canvas = GetComponentInParent<Canvas>();
            m_CanvasRect = canvas != null ? (RectTransform)canvas.rootCanvas.transform : (RectTransform)transform;
            m_Layout = panel != null ? panel.GetComponent<LayoutGroup>() : null;
            group.alpha = 0f;
            group.blocksRaycasts = false;
            group.interactable = false;
            // Icons inside text name themselves on hover (every scene with a tooltip gets this).
            if (GetComponent<InlineIconTooltips>() == null) gameObject.AddComponent<InlineIconTooltips>();
        }

        void OnDestroy()
        {
            if (s_Active == this) s_Active = null;
        }

        public static void Show(string text, object owner = null)
        {
            if (s_Active == null || string.IsNullOrEmpty(text)) return;
            s_Active.BeginShow();
            s_Active.m_BaseText = text;
            s_Active.m_BaseOwner = owner;
            s_Active.Refresh();
        }

        public static void Hide(object owner = null)
        {
            if (s_Active == null) return;
            if (owner != null && s_Active.m_BaseOwner != owner)
            {
                // An override owner leaving through Hide (e.g. disabled) only clears its own layer.
                if (s_Active.m_OverrideOwner == owner) ClearOverride(owner);
                return;
            }
            s_Active.m_BaseText = null;
            s_Active.m_BaseOwner = null;
            s_Active.Refresh();
        }

        /// <summary>Shows text above the base tooltip until <see cref="ClearOverride"/> (the base comes back afterwards).</summary>
        public static void ShowOverride(string text, object owner)
        {
            if (s_Active == null || string.IsNullOrEmpty(text)) return;
            s_Active.BeginShow();
            s_Active.m_OverrideText = text;
            s_Active.m_OverrideOwner = owner;
            s_Active.Refresh();
        }

        public static void ClearOverride(object owner)
        {
            if (s_Active == null || s_Active.m_OverrideOwner != owner) return;
            s_Active.m_OverrideText = null;
            s_Active.m_OverrideOwner = null;
            s_Active.Refresh();
        }

        void BeginShow()
        {
            if (!Visible) m_ShowAt = Time.unscaledTime + 0.3f;
        }

        void Refresh()
        {
            string text = m_OverrideText ?? m_BaseText;
            if (text != null && text != m_Shown) Present(text);
            m_Shown = text;
        }

        /// <summary>Splits the title row from the body, fills both texts and sizes the card to them.</summary>
        void Present(string text)
        {
            TipText.TrySplit(text, out string title, out string body);
            // No title text in this scene: the title is the body's first line.
            if (header == null && title != null)
            {
                body = string.IsNullOrEmpty(body) ? title : title + "\n" + body;
                title = null;
            }

            bool hasTitle = !string.IsNullOrEmpty(title);
            bool hasBody = !string.IsNullOrEmpty(body);
            if (header != null)
            {
                header.gameObject.SetActive(hasTitle);
                header.text = hasTitle ? title : string.Empty;
            }
            if (divider != null) divider.gameObject.SetActive(hasTitle && hasBody);
            label.gameObject.SetActive(hasBody || !hasTitle);
            label.text = hasBody ? body : string.Empty;
            FitWidth(hasTitle ? title : null, hasBody ? body : null);
        }

        /// <summary>As wide as the longest line needs, between <see cref="minWidth"/> and <see cref="maxWidth"/>.</summary>
        void FitWidth(string title, string body)
        {
            if (panel == null || maxWidth <= 0f) return;
            float padding = m_Layout != null ? m_Layout.padding.horizontal : 0f;
            float inside = Mathf.Max(40f, maxWidth - padding);
            float widest = 0f;
            // Preferred widths are measured without wrapping: the longest line as it is.
            if (title != null && header != null) widest = Mathf.Max(widest, header.GetPreferredValues(title, inside, 0f).x);
            if (body != null) widest = Mathf.Max(widest, label.GetPreferredValues(body, inside, 0f).x);
            float width = Mathf.Clamp(Mathf.Ceil(widest) + padding + 2f, Mathf.Min(minWidth, maxWidth), maxWidth);
            panel.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width);
        }

        void LateUpdate()
        {
            float target = Visible && Time.unscaledTime >= m_ShowAt ? 1f : 0f;
            group.alpha = Mathf.MoveTowards(group.alpha, target, Time.unscaledDeltaTime * 10f);
            if (group.alpha <= 0f) return;

            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(m_CanvasRect, InputHelper.PointerPosition, null, out var local)) return;

            var canvasSize = m_CanvasRect.rect.size;
            var size = panel.rect.size;
            var position = local + new Vector2(22f, -22f);

            // Keep the panel fully inside the canvas (pivot is top-left).
            float maxX = canvasSize.x * 0.5f - size.x - 8f;
            float minY = -canvasSize.y * 0.5f + size.y + 8f;
            if (position.x > maxX) position.x = local.x - size.x - 22f;
            if (position.y < minY) position.y = minY;
            position.x = Mathf.Max(position.x, -canvasSize.x * 0.5f + 8f);
            position.y = Mathf.Min(position.y, canvasSize.y * 0.5f - 8f);

            panel.anchoredPosition = position;
        }
    }
}
