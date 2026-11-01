using System;
using DeckParty.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DeckParty.UI
{
    /// <summary>Selectable scenario entry in the single-player list.</summary>
    public sealed class ScenarioItemView : MonoBehaviour
    {
        [SerializeField] internal Button button;
        [SerializeField] internal Image background;
        [SerializeField] internal TMP_Text titleLabel;
        [SerializeField] internal TMP_Text subtitleLabel;
        [SerializeField] internal GameObject clearedIcon;
        [SerializeField] internal GameObject selectedOutline;

        public int ScenarioId { get; private set; }
        public event Action<ScenarioItemView> Clicked;

        bool m_Hooked;

        void Awake() => Hook();

        void Hook()
        {
            if (m_Hooked) return;
            m_Hooked = true;
            button.onClick.AddListener(() => Clicked?.Invoke(this));
        }

        public void Bind(ScenarioDefinition scenario, bool cleared, string extra)
        {
            Hook();
            ScenarioId = scenario.Id;
            string tagline = ContentText.ScenarioTagline(scenario.Id);
            titleLabel.text = ContentText.ScenarioName(scenario.Id);
            subtitleLabel.text = string.IsNullOrEmpty(extra) ? tagline : tagline + "  -  " + extra;
            clearedIcon.SetActive(cleared);
        }

        public void SetSelected(bool selected)
        {
            selectedOutline.SetActive(selected);
            if (background != null) background.color = selected ? UIPalette.PanelAlt : UIPalette.Panel;
        }
    }
}
