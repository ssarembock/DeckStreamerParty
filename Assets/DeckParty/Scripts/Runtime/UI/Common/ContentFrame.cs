using UnityEngine;

namespace DeckParty.UI
{
    /// <summary>
    /// Keeps the game UI readable on any screen shape with small adjustments instead of separate layouts:
    /// the UI is capped at <see cref="maxAspect"/> (21:9) and centred, so on super-ultrawide the stream setup shows at
    /// the sides; and the optional side panel widens gently on wider screens, with the main area following it. While the
    /// side panel shows nothing (single player with the Activity log off) the main area takes the whole width.
    /// </summary>
    [ExecuteAlways]
    [RequireComponent(typeof(RectTransform))]
    public sealed class ContentFrame : MonoBehaviour
    {
        [Tooltip("Widest shape the UI is allowed to take (21:9 = 2.333). Wider screens get side margins.")]
        public float maxAspect = 21f / 9f;
        [Tooltip("Optional side panel (anchored to the right edge) that widens with the screen.")]
        public RectTransform sidePanel;
        [Tooltip("Optional main area that ends where the side panel starts.")]
        public RectTransform mainArea;
        public float minSideWidth = 420f;
        public float maxSideWidth = 520f;
        [Tooltip("Gap between the main area and the side panel, and the screen edge margin.")]
        public float gap = 16f;

        RectTransform m_Rect;
        Vector2 m_LastParentSize;
        bool m_LastSideUsed;

        void OnEnable()
        {
            m_Rect = (RectTransform)transform;
            Apply();
        }

        void Update()
        {
            var parent = m_Rect != null ? m_Rect.parent as RectTransform : null;
            if (parent != null && (parent.rect.size != m_LastParentSize || SideUsed() != m_LastSideUsed)) Apply();
        }

        void OnRectTransformDimensionsChange()
        {
            if (isActiveAndEnabled) Apply();
        }

        public void Apply()
        {
            if (m_Rect == null) m_Rect = (RectTransform)transform;
            var parent = m_Rect.parent as RectTransform;
            if (parent == null) return;
            var size = parent.rect.size;
            m_LastParentSize = size;
            if (size.x <= 1f || size.y <= 1f) return;

            float width = Mathf.Min(size.x, size.y * maxAspect);
            float margin = (size.x - width) * 0.5f;
            m_Rect.anchorMin = Vector2.zero;
            m_Rect.anchorMax = Vector2.one;
            m_Rect.offsetMin = new Vector2(margin, 0f);
            m_Rect.offsetMax = new Vector2(-margin, 0f);

            if (sidePanel == null) return;
            float side = Mathf.Lerp(minSideWidth, maxSideWidth, Mathf.InverseLerp(1700f, 2520f, width));
            sidePanel.offsetMin = new Vector2(-side - gap, sidePanel.offsetMin.y);
            sidePanel.offsetMax = new Vector2(-gap, sidePanel.offsetMax.y);
            m_LastSideUsed = SideUsed();
            if (mainArea != null) mainArea.offsetMax = new Vector2(m_LastSideUsed ? -side - gap * 2f : -gap, mainArea.offsetMax.y);
        }

        /// <summary>Whether the side panel shows anything (a visible child other than its layout spacer).</summary>
        bool SideUsed()
        {
            if (sidePanel == null || !sidePanel.gameObject.activeSelf) return false;
            foreach (Transform child in sidePanel)
                if (child.gameObject.activeSelf && child.name != "Spacer") return true;
            return false;
        }
    }
}
