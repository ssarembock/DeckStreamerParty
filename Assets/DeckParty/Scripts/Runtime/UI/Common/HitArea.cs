using UnityEngine;
using UnityEngine.UI;

namespace DeckParty.UI
{
    /// <summary>
    /// An invisible graphic that only catches the pointer: put it behind a clickable thing whose own pictures don't (a chat
    /// avatar's circle, ring and stats are all built with raycasts off), so a click or hover anywhere on it counts. It draws
    /// nothing, so it costs no fill.
    /// </summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class HitArea : MaskableGraphic
    {
        protected override void Awake()
        {
            base.Awake();
            raycastTarget = true;
            // In the editor GetComponent can hand back a stand-in, so make sure the renderer really exists (see InnerOutline).
            if (!TryGetComponent<CanvasRenderer>(out _)) gameObject.AddComponent<CanvasRenderer>();
        }

        protected override void OnPopulateMesh(VertexHelper vh) => vh.Clear();

        /// <summary>
        /// Gives <paramref name="root"/> a full-size hit area as its first child (behind everything else, so buttons and icons
        /// on top still get their own clicks), unless it already has one or a graphic of its own.
        /// </summary>
        public static HitArea Ensure(RectTransform root)
        {
            if (root == null || root.GetComponent<Graphic>() != null) return null;
            for (int i = 0; i < root.childCount; i++)
                if (root.GetChild(i).TryGetComponent<HitArea>(out var existing)) return existing;
            var rect = new GameObject("HitArea", typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(root, false);
            rect.SetAsFirstSibling();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            return rect.gameObject.AddComponent<HitArea>();
        }
    }
}
