using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace DeckParty.UI
{
    /// <summary>
    /// The full-screen image for a stream that ends badly: Platform Ban (too much Platform Ban Risk) or Server Down (chat
    /// over the Sub Cap). The images come from the Game Database (Stream Endings); while one isn't assigned the game shows
    /// its usual text banner instead. A click (or a couple of seconds) dismisses it.
    /// </summary>
    public sealed class OutcomeSplashView : MonoBehaviour, IPointerClickHandler
    {
        [SerializeField] internal GameObject root;
        [SerializeField] internal CanvasGroup group;
        [SerializeField] internal Image art;
        [SerializeField] internal TMP_Text caption;
        [SerializeField] internal float duration = 2.4f;

        Coroutine m_Routine;

        public bool IsShowing => root != null && root.activeSelf;

        void Awake()
        {
            if (root != null) root.SetActive(false);
        }

        /// <summary>Shows the image with a caption. Returns false (showing nothing) when there's no image.</summary>
        public bool Show(Sprite sprite, string text, Color color)
        {
            if (sprite == null || root == null || art == null) return false;
            art.sprite = sprite;
            art.preserveAspect = true;
            if (caption != null)
            {
                caption.text = text;
                caption.color = color;
            }
            root.SetActive(true);
            if (m_Routine != null) StopCoroutine(m_Routine);
            m_Routine = StartCoroutine(Play());
            return true;
        }

        public void Hide()
        {
            if (m_Routine != null) StopCoroutine(m_Routine);
            m_Routine = null;
            if (root != null) root.SetActive(false);
        }

        public void OnPointerClick(PointerEventData eventData) => Hide();

        IEnumerator Play()
        {
            var rect = art.rectTransform;
            float total = Mathf.Max(0.5f, duration * AppSettings.AnimationTimeScale);
            const float fadeIn = 0.18f;
            const float fadeOut = 0.35f;
            for (float t = 0f; t < total; t += Time.unscaledDeltaTime)
            {
                float k = t / total;
                if (group != null) group.alpha = t < fadeIn ? t / fadeIn : t > total - fadeOut ? (total - t) / fadeOut : 1f;
                // Slams in slightly oversized, then settles with a slow push.
                float slam = t < 0.22f ? Mathf.Lerp(1.25f, 0.98f, UITween.EaseOutCubic(t / 0.22f)) : Mathf.Lerp(0.98f, 1.04f, k);
                rect.localScale = Vector3.one * slam;
                yield return null;
            }
            m_Routine = null;
            root.SetActive(false);
        }
    }
}
