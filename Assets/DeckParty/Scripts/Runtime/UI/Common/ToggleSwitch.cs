using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DeckParty.UI
{
    /// <summary>A simple ON/OFF switch button.</summary>
    public sealed class ToggleSwitch : MonoBehaviour
    {
        [SerializeField] internal Button button;
        [SerializeField] internal TMP_Text stateLabel;
        [SerializeField] internal Image fill;

        bool m_Value;
        bool m_Hooked;

        public event Action<bool> Changed;

        void Awake()
        {
            Hook();
            Loc.Changed += Refresh;
        }

        void OnDestroy() => Loc.Changed -= Refresh;

        void Hook()
        {
            if (m_Hooked || button == null) return;
            m_Hooked = true;
            button.onClick.AddListener(() =>
            {
                m_Value = !m_Value;
                Refresh();
                Changed?.Invoke(m_Value);
            });
        }

        public bool Value
        {
            get => m_Value;
            set
            {
                Hook();
                m_Value = value;
                Refresh();
            }
        }

        void Refresh()
        {
            if (stateLabel != null) stateLabel.text = Loc.Get(m_Value ? "common.on" : "common.off");
            if (fill != null) fill.color = m_Value ? UIPalette.Green : UIPalette.Border;
        }
    }
}
