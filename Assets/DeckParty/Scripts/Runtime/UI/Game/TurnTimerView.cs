using System;
using System.Collections.Generic;
using DeckParty.Core;
using DeckParty.Networking;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DeckParty.UI
{
    /// <summary>The Take Turns countdown pill (shown when the host set a turn timer). Fills this RectTransform.</summary>
    public sealed class TurnTimerView : MonoBehaviour
    {
        Image m_Background;
        TMP_Text m_Label;
        bool m_Built;
        int m_LastSecond = -1;

        void Build()
        {
            if (m_Built) return;
            m_Built = true;
            m_Background = UiFactory.Image("Background", transform, UIPalette.PanelDeep.WithAlpha(0.95f), RuntimeSprites.Rounded, true);
            UiFactory.Stretch(m_Background.rectTransform);
            m_Label = UiFactory.Text("Label", transform, 26f, UIPalette.Text, TextAlignmentOptions.Center, true);
            UiFactory.Stretch(m_Label.rectTransform, 4f);
        }

        void Awake() => Build();

        /// <param name="seconds">Seconds left, or negative to hide.</param>
        public void Render(float seconds, string whose, bool mine)
        {
            Build();
            bool show = seconds >= 0f;
            m_Background.enabled = show;
            m_Label.enabled = show;
            if (!show)
            {
                m_LastSecond = -1;
                return;
            }
            int whole = Mathf.CeilToInt(seconds);
            bool warning = whole <= TurnTimers.WarningSeconds;
            m_Label.text = Loc.Get(mine ? "timer.yours" : "timer.theirs", "seconds", whole, "player", whose);
            m_Background.color = (warning ? UIPalette.Red : UIPalette.PanelDeep).WithAlpha(0.95f);
            if (warning && whole != m_LastSecond && whole > 0)
            {
                Fx.Play(FxCue.CounterPunch, transform, 0.6f);
                if (mine) AudioService.PlayPitched(Sfx.Click, 1.6f, 0.8f);
            }
            m_LastSecond = whole;
        }
    }
}
