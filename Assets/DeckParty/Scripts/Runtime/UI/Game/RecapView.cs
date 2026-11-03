using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace DeckParty.UI
{
    /// <summary>
    /// The quick summary pop-up after a stream ("Great stream!") and, in multiplayer, after the shop: a white card with a black
    /// border, a short title and one column per player (their streamer's avatar, their name and streamer, then the key changes:
    /// the result, Clout, Cash and Plays for a stream; recruits, Sub Cap, actives and resources for a shop), with grey lines
    /// between the columns. Close it with the button, a click outside, Space, Enter or Esc.
    /// </summary>
    public sealed class RecapView : MonoBehaviour, IPointerClickHandler
    {
        [SerializeField] internal GameObject root;
        [SerializeField] internal RectTransform panel;
        [SerializeField] internal TMP_Text titleLabel;
        [SerializeField] internal TMP_Text subtitleLabel;
        [Tooltip("Holds the columns and the lines between them (a horizontal layout).")]
        [SerializeField] internal RectTransform columnsRoot;
        [Tooltip("Inactive template, cloned once per player.")]
        [SerializeField] internal RecapColumnView columnTemplate;
        [Tooltip("Inactive template of the grey line between two columns.")]
        [SerializeField] internal GameObject dividerTemplate;
        [SerializeField] internal Button closeButton;
        [Tooltip("Width of one player's column; the card grows with the number of players.")]
        [SerializeField] internal float columnWidth = 250f;
        [SerializeField] internal float minPanelWidth = 640f;
        [SerializeField] internal float maxPanelWidth = 1680f;
        [Tooltip("The card's width beyond the columns (padding on both sides).")]
        [SerializeField] internal float panelPadding = 90f;

        readonly List<RecapColumnView> m_Columns = new List<RecapColumnView>();
        readonly List<GameObject> m_Dividers = new List<GameObject>();
        Coroutine m_Intro;

        public bool IsOpen => root != null && root.activeSelf;

        void Awake()
        {
            if (closeButton != null) closeButton.onClick.AddListener(Close);
            if (columnTemplate != null) columnTemplate.gameObject.SetActive(false);
            if (dividerTemplate != null) dividerTemplate.SetActive(false);
            if (root != null) root.SetActive(false);
        }

        public void Show(string title, Color titleColor, string subtitle, IList<RecapEntry> entries)
        {
            if (root == null || entries == null || entries.Count == 0) return;
            root.SetActive(true);
            if (titleLabel != null)
            {
                titleLabel.text = title;
                titleLabel.color = LightText.Readable(titleColor);
            }
            if (subtitleLabel != null)
            {
                subtitleLabel.text = LightText.Adapt(subtitle ?? string.Empty);
                subtitleLabel.gameObject.SetActive(!string.IsNullOrEmpty(subtitle));
            }

            int count = entries.Count;
            while (m_Columns.Count < count && columnTemplate != null)
            {
                var column = Instantiate(columnTemplate, columnsRoot);
                column.name = "Column" + (m_Columns.Count + 1);
                m_Columns.Add(column);
            }
            while (m_Dividers.Count < count - 1 && dividerTemplate != null)
            {
                var divider = Instantiate(dividerTemplate, columnsRoot);
                divider.name = "Divider" + (m_Dividers.Count + 1);
                m_Dividers.Add(divider);
            }

            // Column, line, column, line... in that order inside the layout.
            int sibling = 0;
            for (int i = 0; i < m_Columns.Count; i++)
            {
                bool on = i < count;
                m_Columns[i].gameObject.SetActive(on);
                if (!on) continue;
                m_Columns[i].transform.SetSiblingIndex(sibling++);
                m_Columns[i].Bind(entries[i]);
                if (i < count - 1 && i < m_Dividers.Count)
                {
                    m_Dividers[i].SetActive(true);
                    m_Dividers[i].transform.SetSiblingIndex(sibling++);
                }
            }
            for (int i = Mathf.Max(0, count - 1); i < m_Dividers.Count; i++) m_Dividers[i].SetActive(false);

            if (panel != null)
            {
                float width = Mathf.Clamp(count * columnWidth + panelPadding, minPanelWidth, maxPanelWidth);
                panel.sizeDelta = new Vector2(width, panel.sizeDelta.y);
            }
            if (columnsRoot != null) LayoutRebuilder.ForceRebuildLayoutImmediate(columnsRoot);

            if (m_Intro != null) StopCoroutine(m_Intro);
            m_Intro = StartCoroutine(Intro(count));
            Fx.Play(FxCue.MenuIn, panel != null ? panel : root.transform);
            AudioService.Play(Sfx.Notify);
        }

        /// <summary>The card pops in, then the columns follow one after another.</summary>
        IEnumerator Intro(int count)
        {
            if (panel != null)
            {
                panel.localScale = Vector3.one * 0.85f;
                yield return UITween.Run(0.22f, t => panel.localScale = Vector3.one * Mathf.LerpUnclamped(0.85f, 1f, UITween.EaseOutBack(t)));
            }
            for (int i = 0; i < count && i < m_Columns.Count; i++)
            {
                UITween.PopIn(this, m_Columns[i].transform, 0.24f, i * 0.07f, 0.7f);
            }
            m_Intro = null;
        }

        public void Close()
        {
            if (!IsOpen) return;
            if (m_Intro != null)
            {
                StopCoroutine(m_Intro);
                m_Intro = null;
            }
            if (panel != null) panel.localScale = Vector3.one;
            root.SetActive(false);
            AudioService.Play(Sfx.Click);
        }

        /// <summary>A click on the dimmed background (outside the card) closes it.</summary>
        public void OnPointerClick(PointerEventData eventData)
        {
            if (!IsOpen || panel == null) return;
            if (!RectTransformUtility.RectangleContainsScreenPoint(panel, eventData.position, eventData.pressEventCamera)) Close();
        }
    }
}
