using TMPro;
using UnityEngine;

namespace DeckParty.UI
{
    /// <summary>Hover tooltip that follows the pointer and stays on screen.</summary>
    public sealed class TooltipView : MonoBehaviour
    {
        [SerializeField] internal RectTransform panel;
        [SerializeField] internal TMP_Text label;
        [SerializeField] internal CanvasGroup group;

        static TooltipView s_Active;
        RectTransform m_CanvasRect;
        bool m_Visible;
        float m_ShowAt;
        object m_Owner;

        void Awake()
        {
            s_Active = this;
            var canvas = GetComponentInParent<Canvas>();
            m_CanvasRect = canvas != null ? (RectTransform)canvas.rootCanvas.transform : (RectTransform)transform;
            group.alpha = 0f;
            group.blocksRaycasts = false;
            group.interactable = false;
        }

        void OnDestroy()
        {
            if (s_Active == this) s_Active = null;
        }

        public static void Show(string text, object owner = null)
        {
            if (s_Active == null || string.IsNullOrEmpty(text)) return;
            s_Active.label.text = text;
            s_Active.m_Owner = owner;
            if (!s_Active.m_Visible) s_Active.m_ShowAt = Time.unscaledTime + 0.3f;
            s_Active.m_Visible = true;
        }

        public static void Hide(object owner = null)
        {
            if (s_Active == null) return;
            if (owner != null && s_Active.m_Owner != owner) return;
            s_Active.m_Visible = false;
            s_Active.m_Owner = null;
        }

        void LateUpdate()
        {
            float target = m_Visible && Time.unscaledTime >= m_ShowAt ? 1f : 0f;
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
