using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DeckParty.UI
{
    /// <summary>"&lt; Value &gt;" carousel used for scenarios, venues, modes and night counts.</summary>
    public sealed class OptionSelector : MonoBehaviour
    {
        [SerializeField] internal Button prevButton;
        [SerializeField] internal Button nextButton;
        [SerializeField] internal TMP_Text valueLabel;
        [SerializeField] internal TMP_Text titleLabel;

        readonly List<string> m_Options = new List<string>();
        int m_Index;
        bool m_Interactable = true;
        bool m_Hooked;

        public event Action<int> Changed;

        public int Index => m_Index;
        public int Count => m_Options.Count;

        void Awake() => Hook();

        void Hook()
        {
            if (m_Hooked) return;
            m_Hooked = true;
            if (prevButton != null) prevButton.onClick.AddListener(() => Step(-1));
            if (nextButton != null) nextButton.onClick.AddListener(() => Step(1));
        }

        public void SetTitle(string title)
        {
            if (titleLabel != null) titleLabel.text = title;
        }

        public void SetOptions(IList<string> options, int index)
        {
            Hook();
            m_Options.Clear();
            if (options != null) m_Options.AddRange(options);
            m_Index = m_Options.Count == 0 ? 0 : Mathf.Clamp(index, 0, m_Options.Count - 1);
            Refresh();
        }

        public void SetIndex(int index, bool notify = false)
        {
            if (m_Options.Count == 0) return;
            int clamped = Mathf.Clamp(index, 0, m_Options.Count - 1);
            bool changed = clamped != m_Index;
            m_Index = clamped;
            Refresh();
            if (notify && changed) Changed?.Invoke(m_Index);
        }

        public bool Interactable
        {
            get => m_Interactable;
            set
            {
                m_Interactable = value;
                Refresh();
            }
        }

        void Step(int delta)
        {
            if (!m_Interactable || m_Options.Count == 0) return;
            m_Index = (m_Index + delta + m_Options.Count) % m_Options.Count;
            Refresh();
            Changed?.Invoke(m_Index);
        }

        void Refresh()
        {
            if (valueLabel != null) valueLabel.text = m_Options.Count > 0 ? m_Options[m_Index] : "-";
            bool canStep = m_Interactable && m_Options.Count > 1;
            if (prevButton != null) prevButton.interactable = canStep;
            if (nextButton != null) nextButton.interactable = canStep;
        }
    }
}
