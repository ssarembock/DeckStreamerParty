using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DeckParty.UI
{
    public enum ToastKind
    {
        Info,
        Success,
        Warning,
        Error,
    }

    /// <summary>
    /// Short notification banner at the top of the screen. Call <see cref="Show"/> from anywhere. In multiplayer games it
    /// docks at the bottom of the left column instead (<see cref="DockTo"/>, 2026-11-18), where the shop summary appears,
    /// so the menu bar stays clear.
    /// </summary>
    public sealed class ToastView : MonoBehaviour
    {
        [SerializeField] internal CanvasGroup group;
        [SerializeField] internal TMP_Text label;
        [SerializeField] internal Image accent;

        static ToastView s_Active;
        readonly Queue<(string message, ToastKind kind)> m_Queue = new Queue<(string, ToastKind)>();
        Coroutine m_Routine;

        // ------------------------------------------------------------------ docking (multiplayer)

        /// <summary>Gap between the notice and the column's bottom (or the shop summary above it).</summary>
        const float DockGap = 12f;
        const float DockMinHeight = 76f;
        const float DockMaxHeight = 260f;
        const float DockFontSize = 21f;
        static readonly Vector3[] s_Corners = new Vector3[4];
        RectTransform m_Dock;
        RectTransform m_Above;

        /// <summary>
        /// Puts the notices at the bottom of <paramref name="area"/> (the left column), as wide as it, and above
        /// <paramref name="above"/> (the shop summary panel) while that shows. The toast keeps its own canvas; it follows the
        /// column every frame, so screen-shape changes and the shop summary coming and going are handled.
        /// </summary>
        public static void DockTo(RectTransform area, RectTransform above = null)
        {
            if (s_Active == null || area == null) return;
            s_Active.m_Dock = area;
            s_Active.m_Above = above;
            if (s_Active.label != null)
            {
                // Narrow and growing downwards: a fixed size that wraps instead of shrinking to fit one line.
                s_Active.label.enableAutoSizing = false;
                s_Active.label.fontSize = DockFontSize;
                s_Active.label.alignment = TextAlignmentOptions.MidlineLeft;
            }
            s_Active.PlaceDocked();
        }

        void LateUpdate()
        {
            if (m_Dock != null && group != null && group.alpha > 0f) PlaceDocked();
        }

        void PlaceDocked()
        {
            var rect = (RectTransform)transform;
            var parent = rect.parent as RectTransform;
            if (m_Dock == null || parent == null) return;
            m_Dock.GetWorldCorners(s_Corners);
            Vector2 min = parent.InverseTransformPoint(s_Corners[0]);
            Vector2 max = parent.InverseTransformPoint(s_Corners[2]);
            float width = Mathf.Max(120f, max.x - min.x);
            float bottom = min.y;
            if (m_Above != null && m_Above.gameObject.activeInHierarchy)
            {
                m_Above.GetWorldCorners(s_Corners);
                bottom = Mathf.Max(bottom, ((Vector2)parent.InverseTransformPoint(s_Corners[1])).y);
            }

            float height = DockMinHeight;
            if (label != null)
            {
                var margins = label.rectTransform.offsetMin.x - label.rectTransform.offsetMax.x;
                float textHeight = label.GetPreferredValues(label.text, Mathf.Max(40f, width - margins), 0f).y;
                height = Mathf.Clamp(textHeight + 28f, DockMinHeight, DockMaxHeight);
            }
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = Vector2.zero;
            rect.sizeDelta = new Vector2(width, height);
            rect.localPosition = new Vector3(min.x, bottom + DockGap, 0f);
        }

        void Awake()
        {
            s_Active = this;
            group.alpha = 0f;
            group.blocksRaycasts = false;
            group.interactable = false;
        }

        void OnDestroy()
        {
            if (s_Active == this) s_Active = null;
        }

        public static void Show(string message, ToastKind kind = ToastKind.Info)
        {
            if (string.IsNullOrEmpty(message)) return;
            if (s_Active == null)
            {
                Debug.Log($"[DeckParty] {message}");
                return;
            }
            s_Active.Enqueue(message, kind);
        }

        void Enqueue(string message, ToastKind kind)
        {
            while (m_Queue.Count >= 4) m_Queue.Dequeue();
            m_Queue.Enqueue((message, kind));
            if (m_Routine == null) m_Routine = StartCoroutine(Play());
        }

        IEnumerator Play()
        {
            while (m_Queue.Count > 0)
            {
                var (message, kind) = m_Queue.Dequeue();
                label.text = message;
                if (accent != null) accent.color = ColorFor(kind);

                yield return UITween.Run(0.15f, t => group.alpha = t);
                float hold = Mathf.Clamp(1.6f + message.Length * 0.025f, 1.8f, 4f);
                float end = Time.unscaledTime + (m_Queue.Count > 0 ? hold * 0.6f : hold);
                while (Time.unscaledTime < end) yield return null;
                yield return UITween.Run(0.2f, t => group.alpha = 1f - t);
            }
            group.alpha = 0f;
            m_Routine = null;
        }

        static Color ColorFor(ToastKind kind)
        {
            switch (kind)
            {
                case ToastKind.Success: return UIPalette.Green;
                case ToastKind.Warning: return UIPalette.Orange;
                case ToastKind.Error: return UIPalette.Red;
                default: return UIPalette.Teal;
            }
        }
    }
}
