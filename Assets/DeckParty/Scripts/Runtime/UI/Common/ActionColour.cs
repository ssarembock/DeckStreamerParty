using UnityEngine;
using UnityEngine.UI;

namespace DeckParty.UI
{
    /// <summary>
    /// Changes the colour of a text or image on a shop card while the card's viewer has an action (the card's Name turns
    /// white, for example), and back to its own colour otherwise. Only the colour changes: the alpha is kept, so fades still
    /// work. It runs after the UI Skin's text colours, so it wins over them on viewers with an action. In play only (the
    /// prefab keeps its own colour while you edit it).
    /// </summary>
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(100)]
    [AddComponentMenu("Deck Party/Action Colour")]
    public sealed class ActionColour : MonoBehaviour, IActionEffect
    {
        [Tooltip("The colour this text or image turns on viewers with an action (its alpha is ignored).")]
        public Color colour = Color.white;
        [Tooltip("Seconds it takes to change colour (0: at once). A card that switches to another viewer always changes at once.")]
        [Min(0f)] public float fadeTime = 0.15f;

        Graphic m_Graphic;
        Color m_Base;
        bool m_HasBase;
        bool m_Wanted;
        float m_Blend;

        public void SetViewerHasAction(bool hasAction, bool instant)
        {
            if (!Resolve()) return;
            if (!m_HasBase || m_Blend <= 0f) CaptureBase();
            m_Wanted = hasAction;
            if (!instant) return;
            m_Blend = hasAction ? 1f : 0f;
            Apply();
        }

        bool Resolve()
        {
            if (m_Graphic == null) TryGetComponent(out m_Graphic);
            return m_Graphic != null;
        }

        void CaptureBase()
        {
            m_Base = m_Graphic.color;
            m_HasBase = true;
        }

        void OnDisable()
        {
            // Leave the graphic with its own colour.
            if (m_Graphic != null && m_HasBase && m_Blend > 0f)
            {
                m_Blend = 0f;
                Apply();
            }
        }

        // After the views (Update) and the UI Skin's text colours (LateUpdate, order 0), so this colour is the one drawn.
        void LateUpdate()
        {
            if (!Resolve()) return;
            float target = m_Wanted ? 1f : 0f;
            if (m_Blend <= 0f && target <= 0f)
            {
                // Not in use: follow the graphic's own colour, whatever sets it.
                CaptureBase();
                return;
            }
            if (!Mathf.Approximately(m_Blend, target))
            {
                float step = fadeTime > 0f ? Time.unscaledDeltaTime / fadeTime : 1f;
                m_Blend = Mathf.MoveTowards(m_Blend, target, step);
            }
            Apply();
        }

        void Apply()
        {
            if (!m_HasBase) CaptureBase();
            var current = m_Graphic.color;
            var rgb = Color.Lerp(m_Base, colour, m_Blend);
            var wanted = new Color(rgb.r, rgb.g, rgb.b, current.a);
            if (current != wanted) m_Graphic.color = wanted;
        }
    }
}
