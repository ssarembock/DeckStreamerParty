using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DeckParty.UI
{
    /// <summary>Scrolling list of rich-text lines (party log, chat history).</summary>
    public sealed class EventLogView : MonoBehaviour
    {
        [SerializeField] internal ScrollRect scroll;
        [SerializeField] internal RectTransform content;
        [SerializeField] internal TMP_Text linePrefab;
        [SerializeField] internal int maxLines = 120;

        readonly Queue<TMP_Text> m_Lines = new Queue<TMP_Text>();
        Coroutine m_Scroll;

        /// <summary>Fires for every line added (the side panel's tabs count unread lines with it).</summary>
        public event Action LineAdded;

        // Lines added while hidden (an inactive tab) land at the bottom once it's shown again.
        void OnEnable()
        {
            if (m_Lines.Count > 0) m_Scroll = StartCoroutine(ScrollToEnd());
        }

        // Disabling stops the coroutine.
        void OnDisable() => m_Scroll = null;

        public void Add(string richText)
        {
            if (string.IsNullOrEmpty(richText) || linePrefab == null) return;

            TMP_Text line;
            if (m_Lines.Count >= maxLines)
            {
                line = m_Lines.Dequeue();
                line.transform.SetAsLastSibling();
            }
            else
            {
                line = Instantiate(linePrefab, content);
            }

            line.gameObject.SetActive(true);
            line.text = richText;
            m_Lines.Enqueue(line);

            if (isActiveAndEnabled)
            {
                if (m_Scroll != null) StopCoroutine(m_Scroll);
                m_Scroll = StartCoroutine(ScrollToEnd());
            }
            LineAdded?.Invoke();
        }

        public void Clear()
        {
            while (m_Lines.Count > 0)
            {
                var line = m_Lines.Dequeue();
                if (line != null) Destroy(line.gameObject);
            }
        }

        IEnumerator ScrollToEnd()
        {
            yield return null;
            Canvas.ForceUpdateCanvases();
            if (scroll != null) scroll.verticalNormalizedPosition = 0f;
            m_Scroll = null;
        }
    }
}
