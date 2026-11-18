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
        /// <summary>The streamer is still locked (its unlock achievement hasn't been earned): shown dimmed with a lock.</summary>
        public bool Locked { get; private set; }
        public event Action<StreamerTileView> Hovered;
        public event Action<StreamerTileView> Clicked;

        // The lock overlay is made on demand, so tiles built before 2026-11-18 get it without a Rebuild.
        GameObject m_LockOverlay;
        TMP_Text m_LockLabel;

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

        /// <summary>Shows or hides the lock: the tile darkens and says LOCKED (the select screen names what unlocks it).</summary>
        public void SetLocked(bool locked)
        {
            Locked = locked;
            if (locked) EnsureLockOverlay();
            if (m_LockOverlay != null) m_LockOverlay.SetActive(locked);
            if (m_LockLabel != null) m_LockLabel.text = Loc.Get("select.locked");
            var tint = locked ? new Color(0.45f, 0.45f, 0.45f, 1f) : Color.white;
            if (portrait != null) portrait.color = tint;
            if (initialsLabel != null) initialsLabel.alpha = locked ? 0.4f : 1f;
        }

        void EnsureLockOverlay()
        {
            if (m_LockOverlay != null) return;
            var parent = animRoot != null ? animRoot : (RectTransform)transform;
            m_LockOverlay = new GameObject("Lock", typeof(RectTransform));
            var rect = (RectTransform)m_LockOverlay.transform;
            rect.SetParent(parent, false);
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            var shade = m_LockOverlay.AddComponent<Image>();
            shade.color = new Color(0f, 0f, 0f, 0.45f);
            shade.raycastTarget = false;

            var labelObject = new GameObject("Label", typeof(RectTransform));
            var labelRect = (RectTransform)labelObject.transform;
            labelRect.SetParent(rect, false);
            labelRect.anchorMin = new Vector2(0f, 0.35f);
            labelRect.anchorMax = new Vector2(1f, 0.75f);
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;
            m_LockLabel = labelObject.AddComponent<TextMeshProUGUI>();
            if (nameLabel != null) m_LockLabel.font = nameLabel.font;
            m_LockLabel.fontSize = 22;
            m_LockLabel.fontStyle = FontStyles.Bold;
            m_LockLabel.color = Color.white;
            m_LockLabel.alignment = TextAlignmentOptions.Center;
            m_LockLabel.raycastTarget = false;
            m_LockLabel.text = Loc.Get("select.locked");
            // The selection cursor stays on top.
            if (cursor != null) cursor.transform.SetAsLastSibling();
        }
    }
}
