using System;
using DeckParty.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DeckParty.UI
{
    /// <summary>
    /// Stream overlay dressing (nice-to-have N4): a pulsing LIVE badge and the stream's running time, shown during a stream.
    /// Fills this RectTransform (top-right of the board by default).
    /// </summary>
    public sealed class StreamOverlayView : MonoBehaviour
    {
        Image m_Dot;
        TMP_Text m_Live;
        TMP_Text m_Timer;
        bool m_Built;
        float m_StreamStart;
        int m_LastNight = -1;

        void Build()
        {
            if (m_Built) return;
            m_Built = true;
            var badge = UiFactory.Image("Badge", transform, new Color(0.85f, 0.1f, 0.2f, 0.95f), RuntimeSprites.Rounded, true);
            UiFactory.Place(badge.rectTransform, new Vector2(0f, 0f), new Vector2(0f, 1f), Vector2.zero, new Vector2(108f, 0f));
            m_Dot = UiFactory.Image("Dot", badge.transform, Color.white, RuntimeSprites.Circle);
            UiFactory.Place(m_Dot.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(12f, -8f), new Vector2(28f, 8f));
            m_Live = UiFactory.Text("Live", badge.transform, 22f, Color.white, TextAlignmentOptions.MidlineLeft, true);
            UiFactory.Place(m_Live.rectTransform, Vector2.zero, Vector2.one, new Vector2(34f, 0f), new Vector2(-6f, 0f));
            m_Timer = UiFactory.Text("Timer", transform, 20f, UIPalette.Text, TextAlignmentOptions.MidlineLeft);
            UiFactory.Place(m_Timer.rectTransform, Vector2.zero, Vector2.one, new Vector2(118f, 0f), Vector2.zero);
        }

        void Awake() => Build();

        public void Render(PlayerState s)
        {
            Build();
            bool show = FxLibrary.Current.streamOverlay && s != null && s.Phase == GamePhase.Party;
            foreach (Transform child in transform) child.gameObject.SetActive(show);
            if (!show) return;
            if (s.Night != m_LastNight)
            {
                m_LastNight = s.Night;
                m_StreamStart = Time.unscaledTime;
            }
            m_Live.text = Loc.Get("overlay.live");
        }

        void Update()
        {
            if (!m_Built || m_Dot == null || !m_Dot.gameObject.activeInHierarchy) return;
            m_Dot.color = Color.white.WithAlpha(0.5f + 0.5f * Mathf.Abs(Mathf.Sin(Time.unscaledTime * 2.5f)));
            var elapsed = TimeSpan.FromSeconds(Time.unscaledTime - m_StreamStart);
            m_Timer.text = $"{(int)elapsed.TotalMinutes:00}:{elapsed.Seconds:00}";
        }
    }
}
