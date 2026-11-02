using TMPro;
using UnityEngine;

namespace DeckParty.UI
{
    /// <summary>
    /// Hover tooltip that follows the pointer and stays on screen. Two layers: the base tooltip (cards, buttons) and an
    /// override for small things inside them (a community icon on a card, an icon in a line of text). Leaving the small
    /// thing brings the base tooltip back.
    /// </summary>
    public sealed class TooltipView : MonoBehaviour
    {
        [SerializeField] internal RectTransform panel;
        [SerializeField] internal TMP_Text label;
        [SerializeField] internal CanvasGroup group;

        static TooltipView s_Active;
        RectTransform m_CanvasRect;
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
            if (text != null && text != m_Shown) label.text = text;
            m_Shown = text;
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
