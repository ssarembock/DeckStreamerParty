using System.Collections;
using System.Collections.Generic;
using DeckParty.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DeckParty.UI
{
    /// <summary>
    /// Viewers in chat send emojis that float up from their cards and fade away. Which ones, and how often, follow how close
    /// the stream is to a Platform Ban: calm chats send mostly peaceful emojis now and then, tense chats argue, and one
    /// Platform Ban Risk from the limit they get angry and spam. Viewers who carry risk themselves lean angrier.
    /// Art and timing live in the Game Database (Viewer Emojis); purely cosmetic and local (never networked).
    /// </summary>
    public sealed class ViewerEmojiView : MonoBehaviour
    {
        public enum Mood { Peaceful = 0, Argumentative = 1, Angry = 2 }

        [SerializeField] internal HouseView house;
        [Tooltip("Where the floating emojis live (above the cards).")]
        [SerializeField] internal RectTransform layer;

        const int MaxFloating = 24;

        // Chance of each mood (peaceful, argumentative, angry) by danger level, for calm viewers and for risky ones.
        static readonly int[][] s_Calm = { new[] { 80, 18, 2 }, new[] { 45, 45, 10 }, new[] { 10, 40, 50 } };
        static readonly int[][] s_Risky = { new[] { 35, 50, 15 }, new[] { 15, 55, 30 }, new[] { 0, 30, 70 } };
        static readonly string[] s_FallbackText = { ":)", "?!", "#@!" };
        static readonly Color[] s_FallbackColor = { new Color32(107, 227, 139, 255), new Color32(255, 154, 61, 255), new Color32(255, 90, 90, 255) };

        PlayerState m_State;
        bool m_Live;
        float m_NextAt;
        readonly List<GameObject> m_Floating = new List<GameObject>();
        readonly List<GuestCardView> m_Candidates = new List<GuestCardView>();

        /// <summary>Danger level 0 (calm), 1 (tense) or 2 (one Platform Ban Risk from the limit).</summary>
        public static int DangerLevel(PlayerState s)
        {
            if (s == null || s.Phase != GamePhase.Party) return 0;
            int net = PartyEngine.NetTrouble(s);
            if (net <= 0) return 0;
            return net >= GameRules.RaidTroubleThreshold - 1 ? 2 : 1;
        }

        /// <param name="live">False while the board isn't showing a live stream (shop, scoring sequence...).</param>
        public void Render(PlayerState s, bool live)
        {
            m_State = s;
            bool wasLive = m_Live;
            m_Live = live && s != null && s.Phase == GamePhase.Party && s.House.Count > 0;
            if (m_Live && !wasLive) m_NextAt = Time.unscaledTime + Random.Range(0.3f, 1.2f);
        }

        /// <summary>Removes every floating emoji (switching boards, stream over).</summary>
        public void Clear()
        {
            foreach (var go in m_Floating)
                if (go != null) Destroy(go);
            m_Floating.Clear();
            m_Live = false;
        }

        void OnDisable() => Clear();

        void Update()
        {
            if (!m_Live || house == null || layer == null || !FxLibrary.Current.viewerEmojis) return;
            if (Time.unscaledTime < m_NextAt) return;

            var set = GameDatabase.Instance.viewerEmojis;
            int level = DangerLevel(m_State);
            float interval = level == 0 ? set.calmInterval : level == 1 ? set.tenseInterval : set.heatedInterval;
            // Bigger chats chatter more.
            float crowd = Mathf.Clamp(m_State.House.Count / 4f, 1f, 2.5f);
            m_NextAt = Time.unscaledTime + interval / crowd * Random.Range(0.6f, 1.4f) * AppSettings.AnimationTimeScale;
            Spawn(set, level);
        }

        void Spawn(ViewerEmojiSet set, int level)
        {
            m_Floating.RemoveAll(go => go == null);
            if (m_Floating.Count >= MaxFloating) return;

            m_Candidates.Clear();
            foreach (var card in house.Cards)
                if (card.gameObject.activeInHierarchy && !card.IsEmpty && m_State.House.Contains(card.GuestId)) m_Candidates.Add(card);
            if (m_Candidates.Count == 0) return;

            var chosen = m_Candidates[Random.Range(0, m_Candidates.Count)];
            var guest = m_State.Find(chosen.GuestId);
            bool risky = guest != null && PartyEngine.HasTroubleProperty(m_State, guest);
            var mood = PickMood(level, risky);

            var go = CreateFloater(set, mood);
            if (go == null) return;
            m_Floating.Add(go);

            // Start just above the card's head, a little off-centre.
            var cardRect = chosen.AnimRoot;
            var local = new Vector3(Random.Range(cardRect.rect.xMin, cardRect.rect.xMax) * 0.45f, cardRect.rect.yMax * 0.75f, 0f);
            var world = cardRect.TransformPoint(local);
            var rect = (RectTransform)go.transform;
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.localPosition = layer.InverseTransformPoint(world);
            float scale = house.CardScale;
            rect.sizeDelta = Vector2.one * set.size * Mathf.Lerp(0.8f, 1.1f, Mathf.InverseLerp(HouseView.MinCardScale, HouseView.MaxCardScale, scale));
            StartCoroutine(Rise(rect, go.GetComponent<Graphic>(), set.rise, set.lifetime * AppSettings.AnimationTimeScale));
        }

        static Mood PickMood(int level, bool risky)
        {
            var weights = (risky ? s_Risky : s_Calm)[Mathf.Clamp(level, 0, 2)];
            int roll = Random.Range(0, weights[0] + weights[1] + weights[2]);
            if (roll < weights[0]) return Mood.Peaceful;
            if (roll < weights[0] + weights[1]) return Mood.Argumentative;
            return Mood.Angry;
        }

        GameObject CreateFloater(ViewerEmojiSet set, Mood mood)
        {
            var sprite = PickSprite(set, mood);
            if (sprite != null)
            {
                var image = UiFactory.Image("Emoji", layer, Color.white, sprite);
                image.preserveAspect = true;
                return image.gameObject;
            }
            if (set.HasArt) return null;

            // No art yet: a little text emoji so the effect is visible while the sprite sheets are being made.
            int index = (int)mood;
            var label = UiFactory.Text("Emoji", layer, 30f, s_FallbackColor[index], TextAlignmentOptions.Center, true);
            label.text = s_FallbackText[index];
            label.outlineWidth = 0.25f;
            label.outlineColor = new Color32(0, 0, 0, 220);
            return label.gameObject;
        }

        /// <summary>A sprite of the wanted mood, falling back to the nearest mood that has art.</summary>
        static Sprite PickSprite(ViewerEmojiSet set, Mood mood)
        {
            var lists = new[] { set.peaceful, set.argumentative, set.angry };
            int start = (int)mood;
            for (int distance = 0; distance < 3; distance++)
            {
                foreach (int index in new[] { start - distance, start + distance })
                {
                    if (index < 0 || index > 2 || lists[index] == null) continue;
                    var list = lists[index];
                    int count = 0;
                    foreach (var s in list) if (s != null) count++;
                    if (count == 0) continue;
                    int pick = Random.Range(0, count);
                    foreach (var s in list)
                    {
                        if (s == null) continue;
                        if (pick-- == 0) return s;
                    }
                }
            }
            return null;
        }

        IEnumerator Rise(RectTransform rect, Graphic graphic, float distance, float lifetime)
        {
            var start = rect.anchoredPosition;
            float sway = Random.Range(-18f, 18f);
            float phase = Random.value * 6f;
            lifetime = Mathf.Max(0.2f, lifetime);
            for (float t = 0f; t < lifetime; t += Time.unscaledDeltaTime)
            {
                if (rect == null) yield break;
                float k = t / lifetime;
                rect.anchoredPosition = start + new Vector2(Mathf.Sin(k * 5f + phase) * 6f + sway * k, distance * UITween.EaseOutCubic(k));
                rect.localScale = Vector3.one * (k < 0.12f ? Mathf.Lerp(0.2f, 1.1f, k / 0.12f) : Mathf.Lerp(1.1f, 0.95f, (k - 0.12f) / 0.88f));
                if (graphic != null)
                {
                    var c = graphic.color;
                    c.a = k < 0.6f ? 1f : 1f - (k - 0.6f) / 0.4f;
                    graphic.color = c;
                }
                yield return null;
            }
            if (rect != null) Destroy(rect.gameObject);
        }
    }
}
