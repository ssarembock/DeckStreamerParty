using System;
using DeckParty.Core;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace DeckParty.UI
{
    /// <summary>One square on the Select Your Streamer roster grid (a streamer, Random or No Streamer).</summary>
    public sealed class StreamerTileView : MonoBehaviour, IPointerEnterHandler
    {
        [SerializeField] internal Button button;
        [SerializeField] internal Image frame;
        [SerializeField] internal Image portrait;
        [SerializeField] internal Image placeholder;
        [SerializeField] internal TMP_Text initialsLabel;
        [SerializeField] internal TMP_Text nameLabel;
        [SerializeField] internal GameObject cursor;
        [SerializeField] internal TMP_Text cursorLabel;
        [SerializeField] internal RectTransform animRoot;

        /// <summary>A StreamerType, or <see cref="StreamerCatalog.RandomPick"/>.</summary>
        public byte Pick { get; private set; }
        public event Action<StreamerTileView> Hovered;
        public event Action<StreamerTileView> Clicked;

        void Awake()
        {
            if (button != null) button.onClick.AddListener(() => Clicked?.Invoke(this));
        }

        public void OnPointerEnter(PointerEventData eventData) => Hovered?.Invoke(this);

        public void Bind(byte pick)
        {
            Pick = pick;
            bool random = pick == StreamerCatalog.RandomPick;
            var type = random ? StreamerType.None : (StreamerType)pick;
            var sprite = random ? null : StreamerVisuals.Portrait(type);

            if (portrait != null)
            {
                portrait.enabled = sprite != null;
                portrait.sprite = sprite;
            }
            if (placeholder != null)
            {
                placeholder.gameObject.SetActive(sprite == null);
                placeholder.color = random ? UIPalette.Purple : StreamerVisuals.Color(type);
            }
            if (initialsLabel != null) initialsLabel.text = random ? "?" : StreamerVisuals.Initials(type);
            if (nameLabel != null) nameLabel.text = random ? Loc.Get("streamer.Random.name") : StreamerVisuals.Name(type);
            SetSelected(false);
        }

        public void SetSelected(bool selected)
        {
            if (cursor != null) cursor.SetActive(selected);
            if (cursorLabel != null) cursorLabel.text = Loc.Get("select.p1");
            if (frame != null) frame.color = selected ? UIPalette.Gold : UIPalette.Border;
        }
    }
}
