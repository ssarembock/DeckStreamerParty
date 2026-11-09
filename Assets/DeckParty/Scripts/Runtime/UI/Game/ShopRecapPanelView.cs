using System.Collections;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;

namespace DeckParty.UI
{
    /// <summary>
    /// Multiplayer: the shop summary as a small panel at the bottom of the left column (under the players), instead of a
    /// pop-up. One block per player (their name, then what they recruited, Sub Cap raises, actives and resource changes). It
    /// stays up for <see cref="visibleSeconds"/> (the pointer over it holds it) and then fades away.
    /// </summary>
    public sealed class ShopRecapPanelView : MonoBehaviour
    {
        [SerializeField] internal CanvasGroup group;
        [SerializeField] internal TMP_Text titleLabel;
        [SerializeField] internal TMP_Text bodyLabel;
        [Tooltip("How long the panel stays before it fades.")]
        [SerializeField] internal float visibleSeconds = 10f;
        [SerializeField] internal float fadeSeconds = 0.8f;

        Coroutine m_Routine;

        void Awake()
        {
            if (group == null) group = GetComponent<CanvasGroup>();
            gameObject.SetActive(false);
        }

        public bool IsShowing => gameObject.activeSelf;

        /// <param name="title">The panel title (which stream's shop).</param>
        public void Show(string title, IList<RecapEntry> entries)
        {
            if (entries == null || entries.Count == 0) return;
            var sb = new StringBuilder();
            for (int i = 0; i < entries.Count; i++)
            {
                var entry = entries[i];
                if (i > 0) sb.Append("\n\n");
                sb.Append("<b>").Append(entry.name).Append("</b>");
                if (!string.IsNullOrEmpty(entry.streamer)) sb.Append("  <size=80%><color=#53535F>").Append(entry.streamer).Append("</color></size>");
                sb.Append("\n<size=88%>").Append(entry.body).Append("</size>");
            }
            if (titleLabel != null) titleLabel.text = title;
            if (bodyLabel != null) bodyLabel.text = LightText.AdaptOn(bodyLabel.transform, sb.ToString());

            gameObject.SetActive(true);
            transform.SetAsLastSibling();
            if (m_Routine != null) StopCoroutine(m_Routine);
            m_Routine = StartCoroutine(Run());
            Fx.Play(FxCue.MenuIn, transform);
        }

        public void Hide()
        {
            if (m_Routine != null) StopCoroutine(m_Routine);
            m_Routine = null;
            gameObject.SetActive(false);
        }

        IEnumerator Run()
        {
            if (group != null) group.alpha = 1f;
            float shown = 0f;
            while (shown < visibleSeconds)
            {
                // Reading it with the pointer over it keeps it up.
                if (!RectTransformUtility.RectangleContainsScreenPoint((RectTransform)transform, InputHelper.PointerPosition, null)) shown += Time.unscaledDeltaTime;
                yield return null;
            }
            for (float t = 0f; t < fadeSeconds; t += Time.unscaledDeltaTime)
            {
                if (group != null) group.alpha = 1f - t / fadeSeconds;
                yield return null;
            }
            m_Routine = null;
            gameObject.SetActive(false);
        }
    }
}
