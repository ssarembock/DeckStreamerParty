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
        [Tooltip("Optional side panel (anchored to the right edge, or the left with Side On Left) that widens with the screen.")]
        public RectTransform sidePanel;
        [Tooltip("The side panel sits at the left edge, before the main area (the players in multiplayer, left of the Stream Chat).")]
        public bool sideOnLeft;
        [Tooltip("Optional main area that ends where the side panel starts.")]
        public RectTransform mainArea;
        [Tooltip("The side panel's width on narrow (16:9) screens. 2026-11-18: 315, three quarters of the old 420, so the stream gets more room.")]
        public float minSideWidth = 315f;
        [Tooltip("The side panel's width on wide (21:9) screens. 2026-11-18: 390, three quarters of the old 520.")]
        public float maxSideWidth = 390f;
        /// <summary>Scenes saved with the old widths (420 / 520) take three quarters of them once (2026-11-18), with no Rebuild.</summary>
        [SerializeField, HideInInspector] int widthRevision;
        const int WidthRevision = 1;
        const float NarrowerSide = 0.75f;
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
            if (widthRevision < WidthRevision)
            {
                widthRevision = WidthRevision;
                if (Mathf.Approximately(minSideWidth, 420f) && Mathf.Approximately(maxSideWidth, 520f))
                {
                    minSideWidth *= NarrowerSide;
                    maxSideWidth *= NarrowerSide;
                }
            }
            float side = Mathf.Lerp(minSideWidth, maxSideWidth, Mathf.InverseLerp(1700f, 2520f, width));
            m_LastSideUsed = SideUsed();
            if (sideOnLeft)
            {
                sidePanel.offsetMin = new Vector2(gap, sidePanel.offsetMin.y);
                sidePanel.offsetMax = new Vector2(gap + side, sidePanel.offsetMax.y);
                if (mainArea != null) mainArea.offsetMin = new Vector2(m_LastSideUsed ? side + gap * 2f : gap, mainArea.offsetMin.y);
                return;
            }
            sidePanel.offsetMin = new Vector2(-side - gap, sidePanel.offsetMin.y);
            sidePanel.offsetMax = new Vector2(-gap, sidePanel.offsetMax.y);
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
