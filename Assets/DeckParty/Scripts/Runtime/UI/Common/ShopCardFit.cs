using UnityEngine;

namespace DeckParty.UI
{
    /// <summary>
    /// Holds one of your own shop cards (UI Skin > Shop > Card prefab) in the shop grid. The card keeps the size you designed
    /// it at, inside a holder that is scaled as a whole to fit the grid's cell, so its texts and pictures keep their
    /// proportions (and the card's own bounce when you buy it isn't affected).
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public sealed class ShopCardFit : MonoBehaviour
    {
        /// <summary>Used when the prefab's root has no fixed size (its anchors stretch).</summary>
        public static readonly Vector2 FallbackSize = new Vector2(180f, 252f);

        [SerializeField] internal RectTransform scaler;
        [SerializeField] internal Vector2 designSize = FallbackSize;

        /// <summary>Makes a copy of the card prefab in this cell and returns its view (added and wired up if the prefab has none).</summary>
        public ShopItemView Spawn(GameObject prefab)
        {
            designSize = DesignSize(prefab);
            scaler = (RectTransform)new GameObject("Scale", typeof(RectTransform)).transform;
            scaler.SetParent(transform, false);
            Centre(scaler, designSize);

            var card = Instantiate(prefab, scaler, false);
            card.name = prefab.name;
            Centre((RectTransform)card.transform, designSize);
            card.transform.localScale = Vector3.one;
            Fit();

            if (!card.TryGetComponent<ShopItemView>(out var view)) view = card.AddComponent<ShopItemView>();
            view.AutoWire();
            return view;
        }

        /// <summary>The size the card was designed at: its root's Width and Height (anchors together), or the fallback.</summary>
        public static Vector2 DesignSize(GameObject prefab)
        {
            if (prefab != null && prefab.transform is RectTransform rect && rect.anchorMin == rect.anchorMax &&
                rect.sizeDelta.x > 1f && rect.sizeDelta.y > 1f)
                return rect.sizeDelta;
            return FallbackSize;
        }

        static void Centre(RectTransform rect, Vector2 size)
        {
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = size;
        }

        void OnEnable() => Fit();

        void OnRectTransformDimensionsChange() => Fit();

        void Fit()
        {
            if (scaler == null) return;
            var cell = ((RectTransform)transform).rect.size;
            if (cell.x <= 0f || cell.y <= 0f) return;
            float scale = Mathf.Min(cell.x / designSize.x, cell.y / designSize.y);
            scaler.localScale = new Vector3(scale, scale, 1f);
        }
    }
}
