using System;
using System.Collections;
using DeckParty.Networking;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DeckParty.UI
{
    /// <summary>
    /// Multiplayer emotes (N5), the way players talk to each other now that text chat is off: a grid of emote buttons
    /// (the sprites from Game Database > Multiplayer Emotes, or text emotes until those exist) that fills this
    /// RectTransform. A sent emote pops up from the sender's avatar in the Players panel on every screen, then floats up and
    /// fades like the viewers' emojis. Floaters live in <see cref="floatArea"/> so they can rise over anything.
    /// </summary>
    public sealed class EmoteView : MonoBehaviour
    {
        [Tooltip("Where emotes float (an overlay layer over the whole screen).")]
        public RectTransform floatArea;
        [Tooltip("Button size in pixels (the grid fits as many per row as it can).")]
        public float buttonSize = 64f;

        public event Action<int> EmoteClicked;

        bool m_Built;
        float m_LastSent;

        void Awake() => Build();

        void Build()
        {
            if (m_Built) return;
            m_Built = true;

            var grid = gameObject.GetComponent<GridLayoutGroup>();
            if (grid == null) grid = gameObject.AddComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(buttonSize, buttonSize);
            grid.spacing = new Vector2(8f, 8f);
            grid.childAlignment = TextAnchor.MiddleCenter;
            grid.constraint = GridLayoutGroup.Constraint.Flexible;

            int count = Emotes.Count;
            for (int i = 0; i < count; i++)
            {
                int index = i;
                var bg = UiFactory.Image("Emote " + (i + 1), transform, UIPalette.PanelAlt, RuntimeSprites.Rounded, true);
                bg.raycastTarget = true;
                var button = bg.gameObject.AddComponent<Button>();
                button.targetGraphic = bg;
                var nav = button.navigation;
                nav.mode = Navigation.Mode.None;
                button.navigation = nav;
                var colors = button.colors;
                colors.highlightedColor = Color.Lerp(Color.white, UIPalette.Pink, 0.35f);
                colors.pressedColor = Color.Lerp(Color.white, UIPalette.Pink, 0.6f);
                button.colors = colors;
                button.onClick.AddListener(() => Send(index));
                var feedback = bg.gameObject.AddComponent<ButtonFeedback>();
                feedback.hoverScale = 1.08f;

                var sprite = Emotes.SpriteAt(i);
                if (sprite != null)
                {
                    var icon = UiFactory.Image("Icon", bg.transform, Color.white, sprite);
                    UiFactory.Stretch(icon.rectTransform, 5f);
                    icon.preserveAspect = true;
                }
                else
                {
                    var label = UiFactory.Text("Label", bg.transform, 18f, UIPalette.Text, TextAlignmentOptions.Center, true);
                    UiFactory.Stretch(label.rectTransform, 3f);
                    label.text = Emotes.TextAt(i);
                    label.fontStyle = FontStyles.Bold;
                }
            }
            if (floatArea == null) floatArea = (RectTransform)transform.root;
        }

        /// <summary>Your recent emote times: the same anti-spam limit the host applies (<see cref="Emotes.Allowed"/>).</summary>
        readonly System.Collections.Generic.List<float> m_Recent = new System.Collections.Generic.List<float>();
        CanvasGroup m_Group;
        float m_LastWarned = -10f;

        void Send(int index)
        {
            float now = Time.unscaledTime;
            if (!Emotes.Allowed(m_Recent, now))
            {
                // A short "slow down" (not more than once every few seconds).
                if (now - m_LastWarned > 3f)
                {
                    m_LastWarned = now;
                    ToastView.Show(Loc.Get("emote.slow_down", "seconds", Mathf.CeilToInt(Emotes.WaitFor(m_Recent, now))), ToastKind.Warning);
                }
                return;
            }
            m_Recent.Add(now);
            m_LastSent = now;
            EmoteClicked?.Invoke(index);
        }

        void Update()
        {
            // The buttons dim while you have to wait.
            if (m_Group == null && !TryGetComponent(out m_Group)) m_Group = gameObject.AddComponent<CanvasGroup>();
            float wait = Emotes.WaitFor(m_Recent, Time.unscaledTime);
            m_Group.alpha = wait > 0f ? 0.45f : 1f;
        }

        /// <summary>
        /// Pops an emote up from <paramref name="anchor"/> (the sender's avatar in the Players panel) and floats it up with
        /// their name. Without an anchor it rises from the bottom of the screen.
        /// </summary>
        public void Show(string sender, Color color, int index, RectTransform anchor)
        {
            Build();
            if (index < 0 || index >= Emotes.Count || floatArea == null) return;

            var root = UiFactory.Rect("FloatingEmote", floatArea);
            root.anchorMin = root.anchorMax = new Vector2(0.5f, 0.5f);
            root.sizeDelta = new Vector2(150f, 150f);

            var sprite = Emotes.SpriteAt(index);
            Graphic main;
            if (sprite != null)
            {
                var image = UiFactory.Image("Icon", root, Color.white, sprite);
                image.preserveAspect = true;
                image.rectTransform.anchorMin = new Vector2(0f, 0.22f);
                image.rectTransform.anchorMax = Vector2.one;
                image.rectTransform.offsetMin = image.rectTransform.offsetMax = Vector2.zero;
                main = image;
            }
            else
            {
                var text = UiFactory.Text("Label", root, 46f, color, TextAlignmentOptions.Center, true);
                text.enableAutoSizing = false;
                text.text = Emotes.TextAt(index);
                text.outlineWidth = 0.25f;
                text.outlineColor = new Color32(0, 0, 0, 220);
                text.rectTransform.anchorMin = new Vector2(0f, 0.22f);
                text.rectTransform.anchorMax = Vector2.one;
                text.rectTransform.offsetMin = text.rectTransform.offsetMax = Vector2.zero;
                main = text;
            }

            var name = UiFactory.Text("Sender", root, 20f, color, TextAlignmentOptions.Center, true);
            name.rectTransform.anchorMin = Vector2.zero;
            name.rectTransform.anchorMax = new Vector2(1f, 0.22f);
            name.rectTransform.offsetMin = name.rectTransform.offsetMax = Vector2.zero;
            name.text = sender;
            name.outlineWidth = 0.25f;
            name.outlineColor = new Color32(0, 0, 0, 220);

            if (anchor != null && anchor.gameObject.activeInHierarchy)
            {
                root.position = anchor.position;
                // Start a little to the left of the avatar so it rises over the board rather than off-screen.
                root.anchoredPosition += new Vector2(-40f, 20f);
            }
            else
            {
                var area = floatArea.rect;
                root.anchoredPosition = new Vector2(UnityEngine.Random.Range(area.xMin * 0.5f, area.xMax * 0.5f), area.yMin + 120f);
            }

            StartCoroutine(Rise(root, main, name, 170f));
            AudioService.PlayPitched(Sfx.Notify, UnityEngine.Random.Range(1.1f, 1.5f), 0.5f);
        }

        IEnumerator Rise(RectTransform rect, Graphic main, Graphic label, float distance)
        {
            var start = rect.anchoredPosition;
            float sway = UnityEngine.Random.Range(-30f, 30f);
            const float duration = 2.2f;
            for (float t = 0f; t < duration; t += Time.unscaledDeltaTime)
            {
                if (rect == null) yield break;
                float k = t / duration;
                rect.anchoredPosition = start + new Vector2(Mathf.Sin(k * 6f) * 10f + sway * k, distance * UITween.EaseOutCubic(k));
                rect.localScale = Vector3.one * (k < 0.12f ? Mathf.Lerp(0.2f, 1.2f, k / 0.12f) : Mathf.Lerp(1.2f, 0.95f, (k - 0.12f) / 0.88f));
                float alpha = k < 0.65f ? 1f : 1f - (k - 0.65f) / 0.35f;
                SetAlpha(main, alpha);
                SetAlpha(label, alpha);
                yield return null;
            }
            if (rect != null) Destroy(rect.gameObject);
        }

        static void SetAlpha(Graphic graphic, float alpha)
        {
            if (graphic == null) return;
            var c = graphic.color;
            c.a = alpha;
            graphic.color = c;
        }
    }
}
