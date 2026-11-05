using System.Collections.Generic;
using DeckParty.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DeckParty.UI
{
    /// <summary>
    /// When a stream is one Deplatform Risk from being deplatformed, the streamer drops in at the bottom of the Stream Chat looking
    /// anxious (their Anxious animation if they have one, otherwise a sweating, trembling portrait), with an
    /// intermittent Feel shake. The panel sits on this object's RectTransform (move or resize it in the Game scene).
    /// </summary>
    public sealed class StreamerAnxietyView : MonoBehaviour
    {
        [Tooltip("Seconds between the intermittent shakes.")]
        public float shakeInterval = 2.6f;
        [Tooltip("Screen shake strength in pixels for each jolt (the Screen Shake setting scales it).")]
        public float shakePixels = 2.5f;
        [Tooltip("Strength of the panel's own jolt (Feel cue Streamer Anxious) each time.")]
        public float panelJolt = 0.6f;
        [Tooltip("Widest the panel gets. It never gets wider than its parent (the Stream Chat window) minus Side Margin on each side.")]
        public float maxWidth = 1480f;
        [Tooltip("Space kept between the panel and the edges of its parent when the parent is narrower than Max Width.")]
        public float sideMargin = 20f;

        RectTransform m_Root;
        RectTransform m_Panel;
        CharacterPortrait m_Portrait;
        Image m_Placeholder;
        TMP_Text m_Initials;
        TMP_Text m_Title;
        TMP_Text m_Sub;
        readonly List<RectTransform> m_Drops = new List<RectTransform>();
        bool m_Built;
        bool m_Showing;
        float m_NextShake;
        StreamerType m_Streamer = (StreamerType)255;

        void Build()
        {
            if (m_Built) return;
            m_Built = true;
            m_Root = (RectTransform)transform;

            m_Panel = UiFactory.Rect("Panel", m_Root);
            UiFactory.Stretch(m_Panel);
            var bg = UiFactory.Image("Background", m_Panel, new Color(0.35f, 0.04f, 0.08f, 0.92f), RuntimeSprites.Rounded, true);
            UiFactory.Stretch(bg.rectTransform);
            var outline = bg.gameObject.AddComponent<Outline>();
            outline.effectColor = UIPalette.Red;
            outline.effectDistance = new Vector2(3f, -3f);

            var frame = UiFactory.Rect("Portrait", m_Panel);
            UiFactory.Place(frame, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(20f, -90f), new Vector2(200f, 90f));
            m_Placeholder = UiFactory.Image("Placeholder", frame, UIPalette.Red, RuntimeSprites.Circle);
            UiFactory.Stretch(m_Placeholder.rectTransform);
            m_Initials = UiFactory.Text("Initials", m_Placeholder.transform, 64f, Color.white, TextAlignmentOptions.Center, true);
            UiFactory.Stretch(m_Initials.rectTransform);
            var art = UiFactory.Image("Art", frame, Color.white);
            UiFactory.Stretch(art.rectTransform);
            art.preserveAspect = true;
            m_Portrait = art.gameObject.AddComponent<CharacterPortrait>();
            m_Portrait.image = art;

            for (int i = 0; i < 3; i++)
            {
                var drop = UiFactory.Image("Sweat" + i, frame, new Color(0.55f, 0.85f, 1f, 0.9f), RuntimeSprites.Circle);
                drop.rectTransform.sizeDelta = new Vector2(18f, 24f);
                m_Drops.Add(drop.rectTransform);
            }

            m_Title = UiFactory.Text("Title", m_Panel, 50f, UIPalette.Text, TextAlignmentOptions.MidlineLeft, true);
            UiFactory.Place(m_Title.rectTransform, new Vector2(0f, 0.5f), new Vector2(1f, 1f), new Vector2(228f, 0f), new Vector2(-28f, -14f));
            m_Sub = UiFactory.Text("Subtitle", m_Panel, 36f, new Color(1f, 0.8f, 0.8f), TextAlignmentOptions.MidlineLeft);
            UiFactory.Place(m_Sub.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0.5f), new Vector2(228f, 14f), new Vector2(-28f, 0f));

            m_Root.gameObject.SetActive(true);
            m_Panel.gameObject.SetActive(false);
            Loc.Changed += Relabel;
        }

        void OnDestroy() => Loc.Changed -= Relabel;

        /// <summary>Shows or hides the anxious streamer. Call on every render with the board being shown.</summary>
        public void Set(bool anxious, StreamerType streamer)
        {
            Build();
            if (!FxLibrary.Current.streamerAnxiety) anxious = false;
            if (anxious && (!m_Showing || streamer != m_Streamer))
            {
                m_Streamer = streamer;
                bool none = streamer == StreamerType.None;
                var sprite = none ? null : StreamerVisuals.Portrait(streamer);
                var art = GameDatabase.Instance.GetStreamerArt(streamer);
                m_Portrait.ShowStreamer(streamer, CharacterState.Anxious);
                m_Placeholder.gameObject.SetActive(sprite == null && (art == null || !art.IsAnimated));
                m_Placeholder.color = none ? UIPalette.Red : StreamerVisuals.Color(streamer);
                m_Initials.text = none ? "!" : StreamerVisuals.Initials(streamer);
                Relabel();
                if (!m_Showing)
                {
                    FitWidth();
                    m_Panel.gameObject.SetActive(true);
                    Fx.Play(FxCue.BannerSlam, m_Panel, 0.7f);
                    AudioService.Play(Sfx.Error, 0.45f);
                    m_NextShake = Time.unscaledTime + 0.4f;
                }
            }
            if (!anxious && m_Showing) m_Panel.gameObject.SetActive(false);
            m_Showing = anxious;
        }

        void Relabel()
        {
            if (m_Title == null) return;
            m_Title.text = Loc.Get("anxious.title");
            m_Sub.text = m_Streamer == StreamerType.None || m_Streamer == (StreamerType)255
                ? Loc.Get("anxious.sub_none")
                : Loc.Get("anxious.sub", "streamer", StreamerVisuals.Name(m_Streamer));
        }

        /// <summary>Max Width, or less when the parent (the chat window) is narrower.</summary>
        void FitWidth()
        {
            if (m_Root == null || maxWidth <= 0f || !(m_Root.parent is RectTransform parent)) return;
            float width = Mathf.Min(maxWidth, parent.rect.width - sideMargin * 2f);
            if (width > 0f && !Mathf.Approximately(m_Root.sizeDelta.x, width)) m_Root.sizeDelta = new Vector2(width, m_Root.sizeDelta.y);
        }

        void Update()
        {
            if (!m_Showing || m_Panel == null) return;
            FitWidth();
            float t = Time.unscaledTime;

            // Nervous tremble of the portrait (on top of any Anxious animation).
            if (m_Portrait != null)
                m_Portrait.transform.parent.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(t * 29f) * 1.4f + Mathf.Sin(t * 11f) * 0.7f);

            // Sweat drops slide off the forehead.
            for (int i = 0; i < m_Drops.Count; i++)
            {
                float phase = Mathf.Repeat(t * 0.9f + i * 0.33f, 1f);
                float side = i % 2 == 0 ? 1f : -1f;
                m_Drops[i].anchoredPosition = new Vector2(side * (45f + i * 9f), 57f - phase * 105f);
                var image = m_Drops[i].GetComponent<Image>();
                image.color = image.color.WithAlpha(phase < 0.8f ? 0.9f : (1f - phase) * 4.5f);
            }

            if (t >= m_NextShake)
            {
                m_NextShake = t + shakeInterval;
                Fx.Play(FxCue.StreamerAnxious, m_Panel, panelJolt);
                Fx.Shake(shakePixels, 0.2f);
            }
        }
    }
}
