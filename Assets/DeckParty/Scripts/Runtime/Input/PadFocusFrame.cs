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
        bool m_Appear = true;
        bool m_Moving;
        Selectable m_Target;
        Vector2 m_FromPosition;
        Vector2 m_FromSize;
        float m_MoveStart;

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
                if (show)
                {
                    ApplySkin();
                    m_Appear = true;
                }
            }
            if (!show) return;

            var look = PadLook.OrDefault;
            var screen = PadNavigator.FocusScreenRect;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(m_Canvas, screen.min, null, out var min) ||
                !RectTransformUtility.ScreenPointToLocalPointInRectangle(m_Canvas, screen.max, null, out var max)) return;
            var targetPosition = (min + max) * 0.5f;
            var targetSize = (max - min) + Vector2.one * (look.focusPadding * 2f);

            // A new target: move there over the UI Skin's move time, eased. The frame appears in place (no move from where it
            // last was), and it follows a target that moves (a scrolling list, a card lifting).
            var focus = PadNavigator.Focus;
            if (m_Appear)
            {
                m_Appear = false;
                m_Target = focus;
                m_Moving = false;
            }
            else if (focus != m_Target)
            {
                m_Target = focus;
                m_FromPosition = m_Position;
                m_FromSize = m_Size;
                m_MoveStart = Time.unscaledTime;
                m_Moving = look.focusMoveTime > 0.001f;
            }

            if (m_Moving)
            {
                float progress = (Time.unscaledTime - m_MoveStart) / look.focusMoveTime;
                if (progress >= 1f) m_Moving = false;
                else
                {
                    float eased = look.Ease(progress);
                    m_Position = Vector2.LerpUnclamped(m_FromPosition, targetPosition, eased);
                    m_Size = Vector2.Max(Vector2.zero, Vector2.LerpUnclamped(m_FromSize, targetSize, eased));
                }
            }
            if (!m_Moving)
            {
                m_Position = targetPosition;
                m_Size = targetSize;
            }

            float pulse = 1f + Mathf.Sin(Time.unscaledTime * Mathf.PI * 2f * look.focusPulseSpeed) * look.focusPulse;
            m_Frame.anchoredPosition = m_Position;
            m_Frame.sizeDelta = m_Size * pulse;
        }
    }
}
