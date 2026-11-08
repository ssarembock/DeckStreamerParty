using DeckParty.Networking;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DeckParty.UI
{
    /// <summary>
    /// The countdown (the stream timer or the shop timer, when the host set them): an hourglass icon (UI Skin > IconTimer)
    /// and the seconds left, on a white pill with a black border. It turns red and ticks for the last few seconds. Hovering
    /// says which timer it is and whose. Fills this RectTransform.
    /// </summary>
    public sealed class TurnTimerView : MonoBehaviour
    {
        Image m_Border;
        Image m_Fill;
        Image m_Icon;
        TMP_Text m_Label;
        TooltipTrigger m_Tooltip;
        bool m_Built;
        int m_LastSecond = -1;

        void Build()
        {
            if (m_Built) return;
            m_Built = true;
            m_Border = UiFactory.Image("Border", transform, SitePalette.Outline, RuntimeSprites.Rounded, true);
            UiFactory.Stretch(m_Border.rectTransform);
            m_Fill = UiFactory.Image("Fill", m_Border.transform, SitePalette.Surface, RuntimeSprites.Rounded);
            UiFactory.Stretch(m_Fill.rectTransform, 3f);

            m_Icon = UiFactory.Image("Hourglass", transform, SitePalette.Text, null);
            m_Icon.preserveAspect = true;
            var iconRect = m_Icon.rectTransform;
            iconRect.anchorMin = new Vector2(0f, 0.5f);
            iconRect.anchorMax = new Vector2(0f, 0.5f);
            iconRect.pivot = new Vector2(0f, 0.5f);
            iconRect.anchoredPosition = new Vector2(12f, 0f);
            iconRect.sizeDelta = new Vector2(34f, 34f);

            m_Label = UiFactory.Text("Seconds", transform, 30f, SitePalette.Text, TextAlignmentOptions.Center, true);
            var labelRect = m_Label.rectTransform;
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = new Vector2(50f, 2f);
            labelRect.offsetMax = new Vector2(-10f, -2f);

            m_Tooltip = m_Border.gameObject.AddComponent<TooltipTrigger>();
            ApplyIcon();
            UISkin.SkinChanged += ApplyIcon;
        }

        void Awake() => Build();

        void OnDestroy() => UISkin.SkinChanged -= ApplyIcon;

        /// <summary>The hourglass from the UI Skin (your art keeps its colours; the placeholder is tinted).</summary>
        void ApplyIcon()
        {
            if (m_Icon == null) return;
            var skin = GameDatabase.Instance.skin;
            var sprite = skin != null ? skin.GetSprite(SkinSlot.IconTimer) : null;
            m_Icon.sprite = sprite;
            m_Icon.enabled = sprite != null;
        }

        /// <param name="seconds">Seconds left, or negative to hide.</param>
        /// <param name="tip">What the hover says (which timer, whose).</param>
        /// <param name="mine">The local player's own timer (it ticks audibly at the end).</param>
        public void Render(float seconds, string tip, bool mine)
        {
            Build();
            bool show = seconds >= 0f;
            m_Border.enabled = show;
            m_Fill.enabled = show;
            m_Label.enabled = show;
            if (m_Icon.sprite != null) m_Icon.enabled = show;
            if (!show)
            {
                m_LastSecond = -1;
                return;
            }
            int whole = Mathf.CeilToInt(seconds);
            bool warning = whole <= TurnTimers.WarningSeconds;
            m_Label.text = whole.ToString();
            var ink = warning ? SitePalette.Danger : SitePalette.Text;
            m_Label.color = ink;
            m_Border.color = warning ? SitePalette.Danger : SitePalette.Outline;
            m_Icon.color = GameIcons.SlotTint(SkinSlot.IconTimer, ink);
            if (m_Tooltip != null) m_Tooltip.Text = tip;
            if (warning && whole != m_LastSecond && whole > 0)
            {
                Fx.Play(FxCue.CounterPunch, transform, 0.6f);
                if (mine) AudioService.PlayPitched(Sfx.Click, 1.6f, 0.8f);
            }
            m_LastSecond = whole;
        }
    }
}
