using DeckParty.Core;
using UnityEngine;
using UnityEngine.UI;

namespace DeckParty
{
    /// <summary>
    /// The picture on a card, shop row or HUD panel. Shows the character's animation (prefab or controller from the Game
    /// Database, rendered by the <see cref="PortraitStage"/>) when one is assigned, otherwise the static portrait sprite.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class CharacterPortrait : MonoBehaviour
    {
        [Tooltip("The static portrait (always the fallback).")]
        public Image image;
        [Tooltip("Shows the character's cell of the portrait stage. Created next to the image when empty.")]
        public RawImage animated;

        GameObject m_Prefab;
        RuntimeAnimatorController m_Controller;
        Sprite m_Sprite;
        float m_Zoom = 1f;
        Vector2 m_Offset;
        bool m_HasArt;
        PortraitStage.Slot m_Slot;
        CharacterState m_Idle = CharacterState.Idle;

        public bool IsAnimated => m_Slot != null;

        /// <summary>The graphic actually on screen (status shader effects go on this).</summary>
        public Graphic Visible => IsAnimated && animated != null ? animated : (Graphic)image;

        /// <summary>Shows a viewer. <paramref name="idle"/> is the state to rest in.</summary>
        public void ShowViewer(GuestType type, CharacterState idle)
        {
            var art = GameDatabase.Instance.GetGuestArt(type);
            Show(art?.characterPrefab, art?.animatorController, GuestVisuals.Portrait(type), art != null ? art.framingZoom : 1f, art != null ? art.framingOffset : Vector2.zero, idle);
        }

        /// <summary>Shows a streamer (HUD panel, select screen).</summary>
        public void ShowStreamer(StreamerType type, CharacterState idle)
        {
            var art = GameDatabase.Instance.GetStreamerArt(type);
            Show(art?.characterPrefab, art?.animatorController, art?.portrait, 1f, Vector2.zero, idle);
        }

        public void Show(GameObject prefab, RuntimeAnimatorController controller, Sprite sprite, float zoom, Vector2 offset, CharacterState idle)
        {
            bool sameArt = m_HasArt && prefab == m_Prefab && controller == m_Controller && sprite == m_Sprite;
            m_Prefab = prefab;
            m_Controller = controller;
            m_Sprite = sprite;
            m_Zoom = zoom;
            m_Offset = offset;
            m_HasArt = true;
            m_Idle = idle;

            if (sameArt && m_Slot != null)
            {
                m_Slot.Driver.SetIdle(idle);
                return;
            }
            Release();
            if ((prefab != null || controller != null) && isActiveAndEnabled) Acquire();
            Refresh();
        }

        public void Clear()
        {
            Release();
            m_HasArt = false;
            m_Prefab = null;
            m_Controller = null;
            m_Sprite = null;
            Refresh();
        }

        /// <summary>Plays a one-shot (Active, Score, Enter...). Returns false when the character has no such state (or isn't animated).</summary>
        public bool Play(CharacterState state) => m_Slot != null && m_Slot.Driver.Play(state);

        public void SetIdle(CharacterState idle)
        {
            m_Idle = idle;
            if (m_Slot != null) m_Slot.Driver.SetIdle(idle);
        }

        void Acquire()
        {
            var stage = PortraitStage.Instance;
            if (stage == null) return;
            m_Slot = stage.Rent(m_Prefab, m_Controller, m_Sprite, m_Zoom, m_Offset);
            if (m_Slot == null) return;
            EnsureRawImage();
            animated.texture = stage.Texture;
            animated.uvRect = m_Slot.Uv;
            m_Slot.Driver.SetIdle(m_Idle);
        }

        void Release()
        {
            if (m_Slot == null) return;
            var stage = PortraitStage.Instance;
            if (stage != null) stage.Release(m_Slot);
            m_Slot = null;
        }

        void Refresh()
        {
            bool anim = m_Slot != null;
            if (animated != null) animated.enabled = anim;
            if (image != null)
            {
                image.enabled = !anim && m_Sprite != null;
                image.sprite = m_Sprite;
            }
        }

        void EnsureRawImage()
        {
            if (animated != null || image == null) return;
            var go = new GameObject("Animated", typeof(RectTransform), typeof(CanvasRenderer), typeof(RawImage));
            var rect = (RectTransform)go.transform;
            rect.SetParent(image.transform.parent, false);
            rect.SetSiblingIndex(image.transform.GetSiblingIndex() + 1);
            var source = (RectTransform)image.transform;
            rect.anchorMin = source.anchorMin;
            rect.anchorMax = source.anchorMax;
            rect.offsetMin = source.offsetMin;
            rect.offsetMax = source.offsetMax;
            rect.pivot = source.pivot;
            animated = go.GetComponent<RawImage>();
            animated.raycastTarget = false;
            // Keep the aspect square so the cell isn't stretched.
            var fitter = go.AddComponent<AspectRatioFitter>();
            fitter.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
            fitter.aspectRatio = 1f;
        }

        void OnEnable()
        {
            if (m_HasArt && m_Slot == null && (m_Prefab != null || m_Controller != null))
            {
                Acquire();
                Refresh();
            }
        }

        // Off-screen cards free their cell so the stage never fills up.
        void OnDisable() => Release();
    }
}
