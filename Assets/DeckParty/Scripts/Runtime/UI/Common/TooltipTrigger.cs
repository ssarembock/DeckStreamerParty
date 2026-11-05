using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace DeckParty.UI
{
    /// <summary>
    /// Shows a tooltip while the pointer is over this graphic. Set <see cref="Provider"/> for live text. Tick
    /// <see cref="overlay"/> for small things inside something that has its own tooltip (a community icon on a card): the
    /// outer tooltip comes back when the pointer leaves.
    /// </summary>
    public sealed class TooltipTrigger : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [SerializeField, TextArea] internal string text;
        [Tooltip("Optional: a strings-CSV key shown (in the current language) when Text is empty.")]
        [SerializeField] internal string textKey;
        [Tooltip("Show above the tooltip of whatever this sits inside, and hand back to it on exit.")]
        [SerializeField] internal bool overlay;

        public Func<string> Provider;

        public string Text
        {
            get => text;
            set => text = value;
        }

        public bool Overlay
        {
            get => overlay;
            set => overlay = value;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            var content = Provider != null ? Provider() : !string.IsNullOrEmpty(text) ? text : !string.IsNullOrEmpty(textKey) ? Loc.Get(textKey) : null;
            if (string.IsNullOrEmpty(content)) return;
            if (overlay) TooltipView.ShowOverride(content, this);
            else TooltipView.Show(content, this);
        }

        public void OnPointerExit(PointerEventData eventData) => Close();

        void OnDisable() => Close();

        void Close()
        {
            if (overlay) TooltipView.ClearOverride(this);
            else TooltipView.Hide(this);
        }
    }
}
