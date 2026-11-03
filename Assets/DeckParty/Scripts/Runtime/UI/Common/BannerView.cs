using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DeckParty.UI
{
    /// <summary>Big centre-screen callout ("POLICE RAID!", "NIGHT 7", "VICTORY!").</summary>
    public sealed class BannerView : MonoBehaviour
    {
        [SerializeField] internal CanvasGroup group;
        [SerializeField] internal RectTransform panel;
        [SerializeField] internal Image background;
        [SerializeField] internal TMP_Text titleLabel;
        [SerializeField] internal TMP_Text subtitleLabel;
        [Tooltip("Website look: a white card with a black border; the callout's colour goes on this stripe and (darkened to " +
                 "read on white) on the title, instead of tinting the whole panel.")]
        [SerializeField] internal Image accent;
        [SerializeField] internal bool siteStyle;

        Coroutine m_Routine;

        void Awake()
        {
            group.alpha = 0f;
            group.blocksRaycasts = false;
        }

        public void Show(string title, string subtitle, Color color, float hold = 1.3f)
        {
            if (!isActiveAndEnabled) return;
            if (m_Routine != null) StopCoroutine(m_Routine);
            m_Routine = StartCoroutine(Play(title, subtitle, color, hold));
        }

        IEnumerator Play(string title, string subtitle, Color color, float hold)
        {
            titleLabel.text = title;
            subtitleLabel.text = siteStyle ? LightText.Adapt(subtitle ?? string.Empty) : subtitle ?? string.Empty;
            subtitleLabel.gameObject.SetActive(!string.IsNullOrEmpty(subtitle));
            if (siteStyle)
            {
                titleLabel.color = LightText.Readable(color);
                if (accent != null) accent.color = color;
            }
            else
            {
                titleLabel.color = color;
                if (background != null) background.color = new Color(color.r * 0.25f, color.g * 0.25f, color.b * 0.25f, 0.88f);
            }

            group.alpha = 0f;
            panel.localScale = Vector3.one * 0.6f;
            yield return UITween.Run(0.25f, t =>
            {
                group.alpha = t;
                panel.localScale = Vector3.one * Mathf.LerpUnclamped(0.6f, 1f, UITween.EaseOutBack(t));
            });

            float end = Time.unscaledTime + UITween.Scaled(hold);
            while (Time.unscaledTime < end) yield return null;

            yield return UITween.Run(0.25f, t =>
            {
                group.alpha = 1f - t;
                panel.localScale = Vector3.one * (1f + t * 0.08f);
            });
            group.alpha = 0f;
            m_Routine = null;
        }
    }
}
