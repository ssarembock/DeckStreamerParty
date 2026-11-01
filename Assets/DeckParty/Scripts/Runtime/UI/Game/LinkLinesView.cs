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
    /// Link lines (nice-to-have N14): a pulsing heart-pink line between Overly Attached Girlfriend and her partner, and a
    /// rainbow line along an unbroken Nyan Cat chain. Lives on a full-screen RectTransform under the cards.
    /// </summary>
    public sealed class LinkLinesView : MonoBehaviour
    {
        readonly List<(Image line, RectTransform a, RectTransform b, bool rainbow)> m_Lines = new List<(Image, RectTransform, RectTransform, bool)>();
        int m_Used;

        public void Begin() => m_Used = 0;

        public void Link(RectTransform a, RectTransform b, bool rainbow)
        {
            if (!FxLibrary.Current.linkLines || a == null || b == null) return;
            if (m_Used >= m_Lines.Count)
            {
                var image = UiFactory.Image("Link", transform, Color.white, RuntimeSprites.Square);
                image.rectTransform.pivot = new Vector2(0f, 0.5f);
                m_Lines.Add((image, null, null, false));
            }
            var entry = m_Lines[m_Used];
            m_Lines[m_Used] = (entry.line, a, b, rainbow);
            entry.line.gameObject.SetActive(true);
            m_Used++;
        }

        public void End()
        {
            for (int i = m_Used; i < m_Lines.Count; i++) m_Lines[i].line.gameObject.SetActive(false);
        }

        void LateUpdate()
        {
            var root = (RectTransform)transform;
            for (int i = 0; i < m_Used && i < m_Lines.Count; i++)
            {
                var (line, a, b, rainbow) = m_Lines[i];
                if (a == null || b == null || !a.gameObject.activeInHierarchy || !b.gameObject.activeInHierarchy)
                {
                    line.gameObject.SetActive(false);
                    continue;
                }
                Vector2 from = root.InverseTransformPoint(a.position);
                Vector2 to = root.InverseTransformPoint(b.position);
                var delta = to - from;
                var rect = line.rectTransform;
                rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
                rect.anchoredPosition = from;
                rect.sizeDelta = new Vector2(delta.magnitude, rainbow ? 10f : 6f);
                rect.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg);
                float t = Time.unscaledTime;
                line.color = rainbow ? Color.HSVToRGB(Mathf.Repeat(t * 0.4f + i * 0.13f, 1f), 0.75f, 1f).WithAlpha(0.7f)
                    : UIPalette.Pink.WithAlpha(0.45f + 0.35f * Mathf.Abs(Mathf.Sin(t * 3f)));
            }
        }
    }
}
