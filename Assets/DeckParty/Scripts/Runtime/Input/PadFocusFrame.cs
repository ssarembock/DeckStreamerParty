using UnityEngine;
using UnityEngine.UI;

namespace DeckParty.UI
{
    /// <summary>
    /// The gamepad selector's frame: drawn on its own overlay canvas around whatever <see cref="PadNavigator"/> has
    /// highlighted, gliding between targets and breathing gently. Its art and colour are the UI Skin's Controller selector
    /// frame entry (a rounded outline until then); size, glide and pulse are in UI Skin > Controller.
    /// </summary>
    public sealed class PadFocusFrame : MonoBehaviour
    {
        RectTransform m_Canvas;
        RectTransform m_Frame;
        Image m_Image;
        Vector2 m_Position;
        Vector2 m_Size;
        bool m_Visible;

        /// <summary>Builds the overlay canvas and the frame under <paramref name="parent"/>.</summary>
        internal static PadFocusFrame Create(Transform parent)
        {
            var go = new GameObject("PadSelector", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
            go.transform.SetParent(parent, false);
            var canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            // Above the menus, the game and its pop-ups.
            canvas.sortingOrder = 150;
            var scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            var frameGo = new GameObject("Frame", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            frameGo.transform.SetParent(go.transform, false);
            var frame = go.AddComponent<PadFocusFrame>();
            frame.m_Canvas = (RectTransform)go.transform;
            frame.m_Frame = (RectTransform)frameGo.transform;
            frame.m_Frame.anchorMin = frame.m_Frame.anchorMax = new Vector2(0.5f, 0.5f);
            frame.m_Frame.pivot = new Vector2(0.5f, 0.5f);
            frame.m_Image = frameGo.GetComponent<Image>();
            frame.m_Image.raycastTarget = false;
            frame.ApplySkin();
            frameGo.SetActive(false);
            UISkin.SkinChanged += frame.ApplySkin;
            return frame;
        }

        void OnDestroy() => UISkin.SkinChanged -= ApplySkin;

        void ApplySkin()
        {
            if (m_Image == null) return;
            var look = PadLook.OrDefault;
            var entry = GameDatabase.Instance != null && GameDatabase.Instance.skin != null ? GameDatabase.Instance.skin.Get(SkinSlot.PadFocus) : null;
            m_Image.sprite = entry != null && entry.sprite != null ? entry.sprite : RuntimeSprites.RoundedFrame;
            m_Image.type = entry != null && entry.sprite != null ? entry.imageType : Image.Type.Sliced;
            m_Image.pixelsPerUnitMultiplier = entry != null && entry.sprite != null ? Mathf.Max(0.01f, entry.pixelsPerUnitMultiplier) : 1f;
            m_Image.color = entry != null && entry.overrideColor ? entry.color : look.focusColour;
        }

        void LateUpdate()
        {
            bool show = PadNavigator.Showing;
            if (show != m_Visible)
            {
                m_Visible = show;
                m_Frame.gameObject.SetActive(show);
                if (show) ApplySkin();
            }
            if (!show) return;

            var look = PadLook.OrDefault;
            var screen = PadNavigator.FocusScreenRect;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(m_Canvas, screen.min, null, out var min) ||
                !RectTransformUtility.ScreenPointToLocalPointInRectangle(m_Canvas, screen.max, null, out var max)) return;
            var targetPosition = (min + max) * 0.5f;
            var targetSize = (max - min) + Vector2.one * (look.focusPadding * 2f);

            // Glide to a new target; snap when it jumped across the screen from nothing.
            float t = look.focusGlide <= 0.001f || m_Size == Vector2.zero ? 1f : 1f - Mathf.Exp(-Time.unscaledDeltaTime / look.focusGlide);
            m_Position = Vector2.Lerp(m_Position, targetPosition, t);
            m_Size = Vector2.Lerp(m_Size, targetSize, t);

            float pulse = 1f + Mathf.Sin(Time.unscaledTime * Mathf.PI * 2f * look.focusPulseSpeed) * look.focusPulse;
            m_Frame.anchoredPosition = m_Position;
            m_Frame.sizeDelta = m_Size * pulse;
        }
    }
}
