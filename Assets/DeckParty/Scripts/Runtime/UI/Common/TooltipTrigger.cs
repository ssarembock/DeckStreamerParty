using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace DeckParty.UI
{
    /// <summary>Shows a tooltip while the pointer is over this graphic. Set <see cref="Provider"/> for live text.</summary>
    public sealed class TooltipTrigger : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [SerializeField, TextArea] internal string text;

        public Func<string> Provider;

        public string Text
        {
            get => text;
            set => text = value;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            var content = Provider != null ? Provider() : text;
            if (!string.IsNullOrEmpty(content)) TooltipView.Show(content, this);
        }

        public void OnPointerExit(PointerEventData eventData) => TooltipView.Hide(this);

        void OnDisable() => TooltipView.Hide(this);
    }
}
