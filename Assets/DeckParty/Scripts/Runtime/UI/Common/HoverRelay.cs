using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace DeckParty.UI
{
    /// <summary>Raises an event when the pointer enters or leaves this object (added at runtime for multiplayer presence).</summary>
    public sealed class HoverRelay : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        public event Action<bool> Hovered;

        public void OnPointerEnter(PointerEventData eventData) => Hovered?.Invoke(true);
        public void OnPointerExit(PointerEventData eventData) => Hovered?.Invoke(false);

        public static HoverRelay On(Component target)
        {
            if (target == null) return null;
            var relay = target.GetComponent<HoverRelay>();
            if (relay == null) relay = target.gameObject.AddComponent<HoverRelay>();
            return relay;
        }
    }
}
