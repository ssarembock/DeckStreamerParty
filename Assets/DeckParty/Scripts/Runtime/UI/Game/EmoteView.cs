using System;
using System.Collections.Generic;
using DeckParty.Core;
using DeckParty.Networking;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DeckParty.UI
{
    /// <summary>
    /// Emotes (nice-to-have N5): emote buttons (multiplayer) and emotes that float up on everyone's screen. The buttons
    /// fill this RectTransform (one row, or a grid of <see cref="columns"/> per row, as beside the facecam); floaters rise
    /// from <see cref="floatArea"/> (or the screen bottom).
    /// </summary>
    public sealed class EmoteView : MonoBehaviour
    {
        [Tooltip("Where emotes float up (defaults to this object's parent canvas area).")]
        public RectTransform floatArea;
        [Tooltip("Buttons per row. 0 = all in one row.")]
        public int columns;

        public event Action<int> EmoteClicked;

        bool m_Built;
        float m_LastSent;

        void Build()
        {
            if (m_Built) return;
            m_Built = true;
            var root = (RectTransform)transform;
            var row = root;
            if (columns > 0) Stack(gameObject.AddComponent<VerticalLayoutGroup>());
            else Stack(gameObject.AddComponent<HorizontalLayoutGroup>());
            for (int i = 0; i < Emotes.All.Length; i++)
            {
                if (columns > 0 && i % columns == 0)
                {
                    row = UiFactory.Rect("Row " + (i / columns), root);
                    Stack(row.gameObject.AddComponent<HorizontalLayoutGroup>());
                }
                int index = i;
                var bg = UiFactory.Image("Emote " + Emotes.All[i], row, UIPalette.PanelAlt, RuntimeSprites.Rounded, true);
                bg.raycastTarget = true;
                var button = bg.gameObject.AddComponent<Button>();
                button.targetGraphic = bg;
                var nav = button.navigation;
                nav.mode = Navigation.Mode.None;
                button.navigation = nav;
                button.onClick.AddListener(() => Send(index));
                var label = UiFactory.Text("Label", bg.transform, 18f, UIPalette.Text, TextAlignmentOptions.Center, true);
                UiFactory.Stretch(label.rectTransform, 2f);
                label.text = Emotes.All[i];
                label.fontStyle = FontStyles.Bold;
            }
            if (floatArea == null) floatArea = (RectTransform)transform.root;
        }

        static void Stack(HorizontalOrVerticalLayoutGroup layout)
        {
            layout.spacing = 4f;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = true;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
        }

        void Awake() => Build();

        void Send(int index)
        {
            if (Time.unscaledTime - m_LastSent < Emotes.Cooldown) return;
            m_LastSent = Time.unscaledTime;
            EmoteClicked?.Invoke(index);
        }

        /// <summary>Floats an emote up the screen with the sender's name.</summary>
        public void Show(string sender, Color color, int index)
        {
            Build();
            if (index < 0 || index >= Emotes.All.Length || floatArea == null) return;
            var label = UiFactory.Text("FloatingEmote", floatArea, 46f, color, TextAlignmentOptions.Center, true);
            label.enableAutoSizing = false;
            label.text = $"{Emotes.All[index]}\n<size=40%><color=#FFFFFF>{sender}</color></size>";
            label.outlineWidth = 0.25f;
            label.outlineColor = new Color32(0, 0, 0, 220);
            var rect = label.rectTransform;
            rect.sizeDelta = new Vector2(240f, 120f);
            var area = floatArea.rect;
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(UnityEngine.Random.Range(area.xMin * 0.7f, area.xMax * 0.7f), area.yMin + 80f);
            StartCoroutine(Rise(rect, label, area.height * 0.7f));
            AudioService.PlayPitched(Sfx.Notify, UnityEngine.Random.Range(1.1f, 1.5f), 0.5f);
        }

        System.Collections.IEnumerator Rise(RectTransform rect, TMP_Text label, float distance)
        {
            var start = rect.anchoredPosition;
            float sway = UnityEngine.Random.Range(-60f, 60f);
            for (float t = 0f; t < 2.4f; t += Time.unscaledDeltaTime)
            {
                if (rect == null) yield break;
                float k = t / 2.4f;
                rect.anchoredPosition = start + new Vector2(Mathf.Sin(k * 6f) * 18f + sway * k, distance * UITween.EaseOutCubic(k));
                rect.localScale = Vector3.one * (k < 0.1f ? Mathf.Lerp(0.3f, 1.15f, k / 0.1f) : Mathf.Lerp(1.15f, 0.9f, k));
                label.alpha = k < 0.75f ? 1f : 1f - (k - 0.75f) / 0.25f;
                yield return null;
            }
            if (rect != null) Destroy(rect.gameObject);
        }
    }
}
