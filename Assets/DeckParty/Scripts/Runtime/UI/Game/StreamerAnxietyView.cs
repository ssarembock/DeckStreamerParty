using System.Collections.Generic;
using DeckParty.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DeckParty.UI
{
    /// <summary>
    /// When a stream is one Platform Ban Risk from a Platform Ban, the streamer drops in at the top of the screen looking
    /// anxious (their Anxious animation if they have one, otherwise a sweating, trembling portrait), with an
    /// intermittent Feel shake. The panel sits on this object's RectTransform (move or resize it in the Game scene).
    /// </summary>
    public sealed class StreamerAnxietyView : MonoBehaviour
    {
        [Tooltip("Seconds between the intermittent shakes.")]
        public float shakeInterval = 1.6f;
        [Tooltip("Screen shake strength in pixels for each jolt (the Screen Shake setting scales it).")]
        public float shakePixels = 4f;

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
            UiFactory.Place(frame, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(12f, -52f), new Vector2(116f, 52f));
            m_Placeholder = UiFactory.Image("Placeholder", frame, UIPalette.Red, RuntimeSprites.Circle);
            UiFactory.Stretch(m_Placeholder.rectTransform);
            m_Initials = UiFactory.Text("Initials", m_Placeholder.transform, 40f, Color.white, TextAlignmentOptions.Center, true);
            UiFactory.Stretch(m_Initials.rectTransform);
            var art = UiFactory.Image("Art", frame, Color.white);
            UiFactory.Stretch(art.rectTransform);
            art.preserveAspect = true;
            m_Portrait = art.gameObject.AddComponent<CharacterPortrait>();
            m_Portrait.image = art;

            for (int i = 0; i < 3; i++)
            {
                var drop = UiFactory.Image("Sweat" + i, frame, new Color(0.55f, 0.85f, 1f, 0.9f), RuntimeSprites.Circle);
                drop.rectTransform.sizeDelta = new Vector2(12f, 16f);
                m_Drops.Add(drop.rectTransform);
            }

            m_Title = UiFactory.Text("Title", m_Panel, 30f, UIPalette.Text, TextAlignmentOptions.MidlineLeft, true);
            UiFactory.Place(m_Title.rectTransform, new Vector2(0f, 0.5f), new Vector2(1f, 1f), new Vector2(130f, 0f), new Vector2(-16f, -8f));
            m_Sub = UiFactory.Text("Subtitle", m_Panel, 22f, new Color(1f, 0.8f, 0.8f), TextAlignmentOptions.MidlineLeft);
            UiFactory.Place(m_Sub.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0.5f), new Vector2(130f, 8f), new Vector2(-16f, 0f));

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
                    m_Panel.gameObject.SetActive(true);
                    Fx.Play(FxCue.BannerSlam, m_Panel);
                    AudioService.Play(Sfx.Error, 0.6f);
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

        void Update()
        {
            if (!m_Showing || m_Panel == null) return;
            float t = Time.unscaledTime;

            // Nervous tremble of the portrait (on top of any Anxious animation).
            if (m_Portrait != null)
                m_Portrait.transform.parent.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(t * 37f) * 2.2f + Mathf.Sin(t * 13f) * 1.2f);

            // Sweat drops slide off the forehead.
            for (int i = 0; i < m_Drops.Count; i++)
            {
                float phase = Mathf.Repeat(t * 0.9f + i * 0.33f, 1f);
                float side = i % 2 == 0 ? 1f : -1f;
                m_Drops[i].anchoredPosition = new Vector2(side * (30f + i * 6f), 38f - phase * 70f);
                var image = m_Drops[i].GetComponent<Image>();
                image.color = image.color.WithAlpha(phase < 0.8f ? 0.9f : (1f - phase) * 4.5f);
            }

            if (t >= m_NextShake)
            {
                m_NextShake = t + shakeInterval;
                Fx.Play(FxCue.StreamerAnxious, m_Panel);
                Fx.Shake(shakePixels, 0.25f);
            }
        }
    }
}
